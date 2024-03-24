using UnityEditor.ShaderGraph;

namespace DELTation.ToonRP.Editor.ShaderGraph
{
    internal static class ToonFields
    {
        public static readonly FieldDescriptor PositionDropOffWs =
            new(string.Empty, "PositionDropOffWS", "_POSITION_DROPOFF_WS 1");
        
        public static readonly FieldDescriptor Normal = new(string.Empty, "Normal", "_NORMALMAP 1");
        public static readonly FieldDescriptor NormalDropOffTs =
            new(string.Empty, "NormalDropOffTS", "_NORMAL_DROPOFF_TS 1");
        public static readonly FieldDescriptor NormalDropOffOS =
            new(string.Empty, "NormalDropOffOS", "_NORMAL_DROPOFF_OS 1");
        public static readonly FieldDescriptor NormalDropOffWs =
            new(string.Empty, "NormalDropOffWS", "_NORMAL_DROPOFF_WS 1");
    }
}