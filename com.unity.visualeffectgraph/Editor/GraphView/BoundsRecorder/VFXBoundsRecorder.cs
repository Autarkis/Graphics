
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

using UnityEditor.VFX;
using UnityEditor.VFX.UI;
using UnityEngine.XR;


namespace UnityEditor.VFX.UI
{
    class VFXBoundsRecorder
    {
        private VisualEffect m_Effect;
        private bool m_IsRecording = false;
        private Dictionary<string,Bounds> m_Bounds;
        private Dictionary<string, bool> m_FirstBound;
        private VFXView m_View;
        private VFXGraph m_Graph;

        public bool isRecording
        {
            get => m_IsRecording;
            set => m_IsRecording = value;
        }

        public Dictionary<string, Bounds> bounds => m_Bounds;

        public IEnumerable<string> systemNames
        {
            get
            {
                foreach (var system in viewableSystems)
                {
                    string systemName = "";
                    try
                    {
                        systemName = m_Graph.systemNames.GetUniqueSystemName(system);
                    }
                    catch
                    {
                        break;
                    }
                    yield return systemName;
                }
            }
        }

        public VFXView view => m_View;

        public enum ExclusionCause
        {
            kNone,
            kAutomatic,
            kManual,
            kGraphComputed,
            kError,
        }

        public static Dictionary<ExclusionCause, string> exclusionCauseString = new Dictionary<ExclusionCause, string>()
        {
            {ExclusionCause.kNone, ""},
            {ExclusionCause.kAutomatic, "(Automatic)"},
            {ExclusionCause.kManual, "(Manual)"},
            {ExclusionCause.kGraphComputed, "(Graph-Computed)"},
            {ExclusionCause.kError, "(Error)"},
        };

        public static Dictionary<ExclusionCause, string> exclusionCauseTooltip = new Dictionary<ExclusionCause, string>()
        {
            {ExclusionCause.kNone, ""},
            {ExclusionCause.kAutomatic, "its Bounds mode is not set to Recorded."},
            {ExclusionCause.kManual, "its Bounds mode is not set to Recorded."},
            {ExclusionCause.kGraphComputed, "its Bounds are set from operators."},
            {ExclusionCause.kError, "an error occured."},
        };

        IEnumerable<VFXDataParticle> viewableSystems
        {
            get
            {
                return m_View.GetAllContexts().Where(c => c.controller.model is VFXBasicInitialize)
                    .Select(c => c.controller.model.GetData())
                    .OfType<VFXDataParticle>();
            }
        }

        VFXDataParticle GetSystem(string systemName)
        {
            return viewableSystems.First(s => m_Graph.systemNames.GetUniqueSystemName(s) == systemName);
        }

        public BoundsSettingMode GetSystemBoundsSettingMode(string systemName)
        {
            return GetSystem(systemName).boundsSettingMode;
        }

        public VFXBoundsRecorder(VisualEffect effect, VFXView view)
        {
            m_View = view;
            m_Graph = m_View.controller.graph;
            m_Effect = effect;
            EditorApplication.update += UpdateBounds;
            SceneView.duringSceneGui += RenderBounds;
            m_FirstBound = new Dictionary<string, bool>();
            m_Bounds = new Dictionary<string, Bounds>();
            foreach (var syst in systemNames)
            {
                m_FirstBound[syst] = true;
            }

            m_Graph.onInvalidateDelegate += OnParamSystemModified;
        }

        public void CleanUp()
        {
            EditorApplication.update -= UpdateBounds;
            SceneView.duringSceneGui -= RenderBounds;
            isRecording = false;
        }


        public bool NeedsToBeRecorded(string systemName)
        {
            try
            {
                return NeedsToBeRecorded(GetSystem(systemName));
            }
            catch
            {
                return false;
            }
        }

        public bool NeedsToBeRecorded(string systemName, out ExclusionCause cause)
        {
            try
            {
                return NeedsToBeRecorded(GetSystem(systemName), out cause);
            }
            catch
            {
                cause = ExclusionCause.kError;
                return false;
            }
        }


        public bool NeedsAnyToBeRecorded()
        {
            foreach(var system in viewableSystems)
            {
                if (NeedsToBeRecorded(system))
                {
                    return true;
                }
            }
            return false;
        }

        bool NeedsToBeRecorded(VFXDataParticle system)
        {
            VFXContext initContext;
            try
            {
                initContext = system.owners.First(m => m is VFXBasicInitialize);
            }
            catch (Exception)
            {
                return false;
            }
            var boundsSlot = initContext.inputSlots.FirstOrDefault(s => s.name == "bounds");
            return system.boundsSettingMode == BoundsSettingMode.Recorded && !boundsSlot.AllChildrenWithLink().Any();
        }

        bool NeedsToBeRecorded(VFXDataParticle system, out ExclusionCause cause)
        {
            VFXContext initContext;
            try
            {
                initContext = system.owners.First(m => m is VFXBasicInitialize);
            }
            catch
            {
                cause = ExclusionCause.kError;
                return false;
            }

            VFXSlot boundsSlot;
            try
            {
                boundsSlot = initContext.inputSlots.FirstOrDefault(s => s.name == "bounds");
                if (boundsSlot.AllChildrenWithLink().Any())
                {
                    cause = ExclusionCause.kGraphComputed;
                    return false;
                }
            }
            catch
            {
                if (system.boundsSettingMode == BoundsSettingMode.Automatic)
                {
                    cause = ExclusionCause.kAutomatic;
                    return false;
                }
                cause = ExclusionCause.kError;
                return false;
            }

            if (system.boundsSettingMode == BoundsSettingMode.Manual)
            {
                cause = ExclusionCause.kManual;
                return false;
            }

            cause = ExclusionCause.kNone;
            return true;
        }

        //If a slot is modified, find what system/particleData it affects, and reset the bounds at next frame
        void OnParamSystemModified(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (isRecording)
            {
                if (cause == VFXModel.InvalidationCause.kParamChanged)
                {
                    if (model is VFXSlot)
                    {
                        var slot = model as VFXSlot;
                        if (slot.name == "bounds")
                            return;
                        foreach (var data in GetAffectedData(slot))
                        {
                            var particleData = data as VFXDataParticle;
                            if (particleData != null)
                            {
                                string systemName = m_Graph.systemNames.GetUniqueSystemName(particleData);
                                m_FirstBound[systemName] = true;
                            }
                        }
                    }
                }

                if (cause == VFXModel.InvalidationCause.kEnableChanged)
                {
                    if (model is VFXBlock)
                    {
                        var block = model as VFXBlock;
                        var particleData = block.GetData();
                        if (particleData != null)
                        {
                            string systemName = m_Graph.systemNames.GetUniqueSystemName(particleData);
                            m_FirstBound[systemName] = true;
                        }
                    }
                }
            }
        }

        IEnumerable<VFXData> GetAffectedData(VFXSlot slot)
        {
            var owner = slot.owner;
            var block = owner as VFXBlock;
            if (block != null && block.enabled)
            {
                yield return block.GetData();
                yield break;
            }
            var ctx = owner as VFXContext;
            if(ctx != null)
            {
                yield return ctx.GetData();
                yield break;
            }

            var op = owner as VFXOperator;
            if (op != null)
            { 
                var outSlots = op.outputSlots;
                foreach (var outSlot in outSlots)
                {
                    foreach (var linkedSlot in outSlot.LinkedSlots)
                    {
                        foreach (var data in GetAffectedData(linkedSlot))
                        {
                            yield return data;
                        }
                    }
                }
            }
            //otherwise the owner is a VFXParameter, and we do not want to reset the bounds in this case
        }

        void UpdateBounds()
        {
            if (m_IsRecording && m_Effect)
            {
                foreach (var system in viewableSystems)
                {
                    string systemName = m_Graph.systemNames.GetUniqueSystemName(system);
                    if (NeedsToBeRecorded(system))
                    {
                        Bounds currentBounds = m_Effect.GetComputedBounds(systemName);
                        if (currentBounds.size == Vector3.zero)
                            continue;
                        if (system.space == VFXCoordinateSpace.World)
                        {
                            Matrix4x4 worldToLocal = m_Effect.transform.worldToLocalMatrix;
                            currentBounds = TransformAABBSlow(currentBounds, worldToLocal);
                        }
                        if (m_FirstBound[systemName])
                        {
                            m_Bounds[systemName] = currentBounds;
                            m_FirstBound[systemName] = false;
                        }
                        else
                        {
                            Bounds tmpBounds = m_Bounds[systemName];
                            tmpBounds.Encapsulate(currentBounds);
                            m_Bounds[systemName] = tmpBounds;
                        }
                    }
                }
            }
        }

        void RenderBounds(SceneView sv)
        {
            //TODO : Render all bounds when the selected system is not "Recorded"
            if (m_IsRecording && m_Effect.gameObject.activeSelf)
            {
                bool renderAllRecordedBounds = false;
                HashSet<string> selectedSystems = new HashSet<string>();
                foreach (var system in viewableSystems)
                {
                    var allSystemContexts = system.owners.ToList();

                    var selectedSystemContexts = m_View.GetAllContexts()
                        .Where(c => c.selected && c.controller.model is VFXBasicInitialize)
                        .Select(c => c.controller.model)
                        .Where(m => allSystemContexts.Contains(m));

                    if (selectedSystemContexts.Any())
                    {
                        string systemName = "";
                        try  //RenderBounds() is not executed in the same thread, so it can be executed before viewableSystems is up-to-date when a system is deleted
                        {
                            systemName = m_Graph.systemNames.GetUniqueSystemName(system);
                        }
                        catch
                        {
                            continue;
                        }

                        selectedSystems.Add(systemName);
                    }
                }

                if (!selectedSystems.Where(NeedsToBeRecorded).Any())
                    renderAllRecordedBounds = true;
                foreach (var system in viewableSystems)
                {
                    
                    string systemName = "";
                    try  //RenderBounds() is not executed in the same thread, so it can be executed before viewableSystems is up-to-date when a system is deleted
                    {
                        systemName = m_Graph.systemNames.GetUniqueSystemName(system);
                    }
                    catch
                    {
                        continue;
                    }
                    
                    if ((selectedSystems.Contains(systemName) || renderAllRecordedBounds) && m_Bounds.ContainsKey(systemName) && NeedsToBeRecorded(system))
                        RenderBoundsSystem(m_Bounds[systemName]);
                }

            }
        }

        private void RenderBoundsSystem(Bounds bounds)
        {
            Matrix4x4 oldMatrix = Handles.matrix;
            Handles.matrix = m_Effect.transform.localToWorldMatrix;

            var points = ExtractVerticesFromBounds(bounds);

            Color prevColor = Handles.color;
            Handles.color = Color.red;
            Handles.DrawLine(points[0], points[1]);
            Handles.DrawLine(points[2], points[3]);
            Handles.DrawLine(points[4], points[5]);
            Handles.DrawLine(points[6], points[7]);

            Handles.DrawLine(points[0], points[2]);
            Handles.DrawLine(points[0], points[4]);
            Handles.DrawLine(points[1], points[3]);
            Handles.DrawLine(points[1], points[5]);

            Handles.DrawLine(points[2], points[6]);
            Handles.DrawLine(points[3], points[7]);
            Handles.DrawLine(points[4], points[6]);
            Handles.DrawLine(points[5], points[7]);
            Handles.matrix = oldMatrix;

            Handles.color = prevColor;
        }

        private Vector3[] ExtractVerticesFromBounds(Bounds bounds)
        {
            Vector3[] points = new Vector3[8];

            Vector3 center = bounds.center;
            Vector3 size = bounds.size;

            points[0] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[1] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[2] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[3] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[4] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[5] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);

            points[6] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[7] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);
            return points;
        }

        private Bounds TransformAABBSlow(Bounds bounds, Matrix4x4 transform)
        {
            Vector3[] v = ExtractVerticesFromBounds(bounds);
            Bounds transformed = new Bounds(transform.MultiplyPoint(v[0]), Vector3.zero);

            for (int i = 1; i < 8; i++)
            {
                Vector3 point = transform.MultiplyPoint(v[i]);
                transformed.Encapsulate(point);
            }
            return transformed;
        }

        public void ModifyMode(string syst, BoundsSettingMode mode)
        {
            VFXDataParticle system = GetSystem(syst);
            system.boundsSettingMode = mode;
            system.OnSettingModified(system.GetSetting("boundsSettingMode"));
            system.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
        }

        public void ToggleRecording()
        {
            m_IsRecording = !m_IsRecording;
            foreach (var system in viewableSystems)
            {
                string systemName = "";
                try  
                {
                    systemName = m_Graph.systemNames.GetUniqueSystemName(system);
                }
                catch
                {
                    continue;
                }
                if (NeedsToBeRecorded(system))
                {
                    system.SetSettingValue("needsComputeBounds", m_IsRecording);
                }
            }

            if (m_IsRecording)
            {
                foreach (var syst in systemNames)
                    m_FirstBound[syst] = true;
            }
        }

        public void ApplyCurrentBounds()
        {
            foreach (var system in viewableSystems)
            {
                string systemName = m_Graph.systemNames.GetUniqueSystemName(system);

                if (m_Bounds.ContainsKey(systemName) && NeedsToBeRecorded(system))
                {
                    VFXContext initContext;
                    try
                    {
                        initContext = system.owners.First(m => m is VFXBasicInitialize);
                    }
                    catch (Exception)
                    {
                        break;
                    }
                    var boundsSlot = initContext.inputSlots.FirstOrDefault(s => s.name == "bounds");
                    if(initContext.GetOutputSpaceFromSlot(boundsSlot) == VFXCoordinateSpace.Local) //This should always be the case
                        boundsSlot.value = new AABox() { center = m_Bounds[systemName].center, size = m_Bounds[systemName].size };
                    else
                    {
                        var localToWorld = m_Effect.transform.localToWorldMatrix;
                        var transformedBounds = TransformAABBSlow(m_Bounds[systemName], localToWorld);
                        boundsSlot.value = new AABox() { center = transformedBounds.center, size = transformedBounds.size };
                    }
                }
            }
        }

        public VFXContextUI GetInitContextController(string systemName)
        {
            var system = GetSystem(systemName);
            VFXContextUI initContextUI;
            try
            {
                initContextUI = m_View.GetAllContexts().First(c =>
                    c.controller.model is VFXBasicInitialize &&
                    m_Graph.systemNames.GetUniqueSystemName(c.controller.model.GetData()) == systemName);
            }
            catch
            {
                throw new InvalidOperationException("The system does not have an Init context.");
            }

            return initContextUI;
        }
    }
}
