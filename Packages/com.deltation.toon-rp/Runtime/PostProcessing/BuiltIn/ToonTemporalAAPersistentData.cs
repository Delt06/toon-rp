using System;
using JetBrains.Annotations;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonTemporalAAPersistentData : IDisposable
    {
        [CanBeNull] public RTHandle HistoryRt;
        public bool HistoryRtStoredValidData { get; private set; }

        public void Dispose()
        {
            HistoryRt?.Release();
        }

        public void OnCapturedHistoryRt() => HistoryRtStoredValidData = true;
    }
}