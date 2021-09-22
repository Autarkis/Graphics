using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;


namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(Ocean))]
    class OceanEditor : VolumeComponentEditor
    {
        // General
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_GridResolution;
        SerializedDataParameter m_GridSize;
        SerializedDataParameter m_WaveAmplitude;
        SerializedDataParameter m_Choppiness;
        SerializedDataParameter m_NumLevelOfDetails;
        SerializedDataParameter m_Material;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Ocean>(serializedObject);
            // General
            m_Enable = Unpack(o.Find(x => x.enable));
            m_GridResolution = Unpack(o.Find(x => x.gridResolution));
            m_GridSize = Unpack(o.Find(x => x.gridSize));
            m_WaveAmplitude = Unpack(o.Find(x => x.waveAmpltiude));
            m_Choppiness = Unpack(o.Find(x => x.choppiness));
            m_NumLevelOfDetails = Unpack(o.Find(x => x.numLevelOfDetais));
            m_Material = Unpack(o.Find(x => x.material));
        }

        void SanitizeVector4(SerializedDataParameter property)
        {
            Vector4 vec4 = property.value.vector4Value;
            vec4.x = Mathf.Max(0, vec4.x);
            vec4.y = Mathf.Max(0, vec4.y);
            vec4.z = Mathf.Max(0, vec4.z);
            vec4.w = Mathf.Max(0, vec4.w);
            property.value.vector4Value = vec4;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("General", EditorStyles.miniLabel);
            PropertyField(m_Enable);
            PropertyField(m_GridResolution);
            PropertyField(m_GridSize);

            EditorGUI.BeginChangeCheck();
            PropertyField(m_WaveAmplitude);
            if (EditorGUI.EndChangeCheck())
                SanitizeVector4(m_WaveAmplitude);

            EditorGUI.BeginChangeCheck();
            PropertyField(m_Choppiness);
            if (EditorGUI.EndChangeCheck())
                SanitizeVector4(m_Choppiness);

            PropertyField(m_NumLevelOfDetails);
            PropertyField(m_Material);
        }
    }
}
