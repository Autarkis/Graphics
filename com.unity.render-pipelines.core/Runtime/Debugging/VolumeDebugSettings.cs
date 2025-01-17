using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Volume debug settings.
    /// </summary>
    public abstract class VolumeDebugSettings<T> : IVolumeDebugSettings
        where T : MonoBehaviour, IAdditionalData
    {
        /// <summary>Current volume component to debug.</summary>
        public int selectedComponent { get; set; } = 0;

        protected int m_SelectedCameraIndex = 0;

        /// <summary>Selected camera index.</summary>
        public int selectedCameraIndex
        {
            get
            {
#if UNITY_EDITOR
                if (m_SelectedCameraIndex < 0 || m_SelectedCameraIndex > additionalCameraDatas.Count + 1)
                    return 0;
#else
                if (m_SelectedCameraIndex < 0 || m_SelectedCameraIndex > additionalCameraDatas.Count)
                    return 0;
#endif
                return m_SelectedCameraIndex;
            }
            set { m_SelectedCameraIndex = value; }
        }

        /// <summary>Current camera to debug.</summary>
        public Camera selectedCamera
        {
            get
            {
#if UNITY_EDITOR
                if (m_SelectedCameraIndex <= 0 || m_SelectedCameraIndex > additionalCameraDatas.Count + 1)
                    return null;
                if (m_SelectedCameraIndex == 1)
                    return SceneView.lastActiveSceneView.camera;
                else
                    return additionalCameraDatas[m_SelectedCameraIndex - 2].GetComponent<Camera>();
#else
                if (m_SelectedCameraIndex <= 0 || m_SelectedCameraIndex > additionalCameraDatas.Count)
                    return null;
                return additionalCameraDatas[m_SelectedCameraIndex - 1].GetComponent<Camera>();
#endif
            }
        }

        /// <summary>Returns the collection of registered cameras.</summary>
        public IEnumerable<Camera> cameras
        {
            get
            {
                foreach (T additionalCameraData in additionalCameraDatas)
                {
                    yield return additionalCameraData.GetComponent<Camera>();
                }
            }
        }

        /// <summary>Selected camera volume stack.</summary>
        public abstract VolumeStack selectedCameraVolumeStack { get; }

        /// <summary>Selected camera volume layer mask.</summary>
        public abstract LayerMask selectedCameraLayerMask { get; }

        /// <summary>Selected camera volume position.</summary>
        public abstract Vector3 selectedCameraPosition { get; }

        /// <summary>Type of the current component to debug.</summary>
        public Type selectedComponentType
        {
            get { return componentTypes[selectedComponent - 1]; }
            set
            {
                var index = componentTypes.FindIndex(t => t == value);
                if (index != -1)
                    selectedComponent = index + 1;
            }
        }

        static List<Type> s_ComponentTypes;

        /// <summary>List of Volume component types.</summary>
        static public List<Type> componentTypes
        {
            get
            {
                if (s_ComponentTypes == null)
                {
                    s_ComponentTypes = VolumeManager.instance.baseComponentTypeArray
                        .Where(t => !t.IsDefined(typeof(HideInInspector), false))
                        .Where(t => !t.IsDefined(typeof(ObsoleteAttribute), false))
                        .OrderBy(t => ComponentDisplayName(t))
                        .ToList();
                }
                return s_ComponentTypes;
            }
        }

        /// <summary>Returns the name of a component from its VolumeComponentMenuForRenderPipeline.</summary>
        /// <param name="component">A volume component.</param>
        /// <returns>The component display name.</returns>
        public static string ComponentDisplayName(Type component)
        {
            if (component.GetCustomAttribute(typeof(VolumeComponentMenuForRenderPipeline), false) is VolumeComponentMenuForRenderPipeline volumeComponentMenuForRenderPipeline)
                return volumeComponentMenuForRenderPipeline.menu;

            if (component.GetCustomAttribute(typeof(VolumeComponentMenu), false) is VolumeComponentMenuForRenderPipeline volumeComponentMenu)
                return volumeComponentMenu.menu;

            return component.Name;
        }

        protected static List<T> additionalCameraDatas { get; private set; } = new List<T>();

        /// <summary>
        /// Register the camera for the Volume Debug.
        /// </summary>
        /// <param name="additionalCamera">The AdditionalCameraData of the camera to be registered.</param>
        public static void RegisterCamera(T additionalCamera)
        {
            if (!additionalCameraDatas.Contains(additionalCamera))
                additionalCameraDatas.Add(additionalCamera);
        }

        /// <summary>
        /// Unregister the camera for the Volume Debug.
        /// </summary>
        /// <param name="additionalCamera">The AdditionalCameraData of the camera to be registered.</param>
        public static void UnRegisterCamera(T additionalCamera)
        {
            if (additionalCameraDatas.Contains(additionalCamera))
                additionalCameraDatas.Remove(additionalCamera);
        }

        internal VolumeParameter GetParameter(VolumeComponent component, FieldInfo field)
        {
            return (VolumeParameter)field.GetValue(component);
        }

        internal VolumeParameter GetParameter(FieldInfo field)
        {
            VolumeStack stack = selectedCameraVolumeStack;
            return stack == null ? null : GetParameter(stack.GetComponent(selectedComponentType), field);
        }

        internal VolumeParameter GetParameter(Volume volume, FieldInfo field)
        {
            var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
            if (!profile.TryGet(selectedComponentType, out VolumeComponent component))
                return null;
            var param = GetParameter(component, field);
            if (!param.overrideState)
                return null;
            return param;
        }

        float[] weights = null;
        float ComputeWeight(Volume volume, Vector3 triggerPos)
        {
            var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;

            if (!volume.gameObject.activeInHierarchy) return 0;
            if (!volume.enabled || profile == null || volume.weight <= 0f) return 0;
            if (!profile.TryGet(selectedComponentType, out VolumeComponent component)) return 0;
            if (!component.active) return 0;

            float weight = Mathf.Clamp01(volume.weight);
            if (!volume.isGlobal)
            {
                var colliders = volume.GetComponents<Collider>();

                // Find closest distance to volume, 0 means it's inside it
                float closestDistanceSqr = float.PositiveInfinity;
                foreach (var collider in colliders)
                {
                    if (!collider.enabled)
                        continue;

                    var closestPoint = collider.ClosestPoint(triggerPos);
                    var d = (closestPoint - triggerPos).sqrMagnitude;

                    if (d < closestDistanceSqr)
                        closestDistanceSqr = d;
                }
                float blendDistSqr = volume.blendDistance * volume.blendDistance;
                if (closestDistanceSqr > blendDistSqr)
                    weight = 0f;
                else if (blendDistSqr > 0f)
                    weight *= 1f - (closestDistanceSqr / blendDistSqr);
            }
            return weight;
        }

        Volume[] volumes = null;

        /// <summary>Get an array of volumes on the <see cref="selectedCameraLayerMask"/></summary>
        /// <returns>An array of volumes sorted by influence.</returns>
        public Volume[] GetVolumes()
        {
            return VolumeManager.instance.GetVolumes(selectedCameraLayerMask)
                .Where(v => v.sharedProfile != null)
                .Reverse().ToArray();
        }

        VolumeParameter[,] savedStates = null;
        VolumeParameter[,] GetStates()
        {
            var fields = selectedComponentType
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Where(t => t.FieldType.IsSubclassOf(typeof(VolumeParameter)))
                .ToArray();

            VolumeParameter[,] states = new VolumeParameter[volumes.Length, fields.Length];
            for (int i = 0; i < volumes.Length; i++)
            {
                var profile = volumes[i].HasInstantiatedProfile() ? volumes[i].profile : volumes[i].sharedProfile;
                if (!profile.TryGet(selectedComponentType, out VolumeComponent component))
                    continue;

                for (int j = 0; j < fields.Length; j++)
                {
                    var param = GetParameter(component, fields[j]); ;
                    states[i, j] = param.overrideState ? param : null;
                }
            }
            return states;
        }

        bool ChangedStates(VolumeParameter[,] newStates)
        {
            if (savedStates.GetLength(1) != newStates.GetLength(1))
                return true;
            for (int i = 0; i < savedStates.GetLength(0); i++)
            {
                for (int j = 0; j < savedStates.GetLength(1); j++)
                {
                    if ((savedStates[i, j] == null) != (newStates[i, j] == null))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Refreshes the volumes, fetches the stored volumes on the panel
        /// </summary>
        /// <param name="newVolumes">The list of <see cref="Volume"/> to refresh</param>
        /// <returns>If the volumes have been refreshed</returns>
        public bool RefreshVolumes(Volume[] newVolumes)
        {
            bool ret = false;
            if (volumes == null || !newVolumes.SequenceEqual(volumes))
            {
                volumes = (Volume[])newVolumes.Clone();
                savedStates = GetStates();
                ret = true;
            }
            else
            {
                var newStates = GetStates();
                if (savedStates == null || ChangedStates(newStates))
                {
                    savedStates = newStates;
                    ret = true;
                }
            }

            var triggerPos = selectedCameraPosition;
            weights = new float[volumes.Length];
            for (int i = 0; i < volumes.Length; i++)
                weights[i] = ComputeWeight(volumes[i], triggerPos);

            return ret;
        }

        /// <summary>
        /// Obtains the volume weight
        /// </summary>
        /// <param name="volume"><see cref="Volume"/></param>
        /// <returns>The weight of the volume</returns>
        public float GetVolumeWeight(Volume volume)
        {
            if (weights == null)
                return 0;

            float total = 0f, weight = 0f;
            for (int i = 0; i < volumes.Length; i++)
            {
                weight = weights[i];
                weight *= 1f - total;
                total += weight;

                if (volumes[i] == volume)
                    return weight;
            }

            return 0f;
        }

        /// <summary>
        /// Return if the <see cref="Volume"/> has influence
        /// </summary>
        /// <param name="volume"><see cref="Volume"/> to check the influence</param>
        /// <returns>If the volume has influence</returns>
        public bool VolumeHasInfluence(Volume volume)
        {
            if (weights == null)
                return false;

            int index = Array.IndexOf(volumes, volume);
            if (index == -1)
                return false;

            return weights[index] != 0f;
        }
    }
}
