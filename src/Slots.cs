// Generated from AMD ADLX headers. Run tools/Generate-AdlxSlots.ps1 after SDK changes.
namespace Radeoner { internal static class Slots {
internal static class IADLXManualFanTuningState {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int GetFanSpeed = 3;
internal const int SetFanSpeed = 4;
internal const int GetTemperature = 5;
internal const int SetTemperature = 6;
}
internal static class IADLXManualFanTuningStateList {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int Size = 3;
internal const int Empty = 4;
internal const int Begin = 5;
internal const int End = 6;
internal const int At = 7;
internal const int Clear = 8;
internal const int Remove_Back = 9;
internal const int Add_Back = 10;
internal const int At_ManualFanTuningStateList = 11;
internal const int Add_Back_ManualFanTuningStateList = 12;
}
internal static class IADLXManualFanTuning {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int GetFanTuningRanges = 3;
internal const int GetFanTuningStates = 4;
internal const int GetEmptyFanTuningStates = 5;
internal const int IsValidFanTuningStates = 6;
internal const int SetFanTuningStates = 7;
internal const int IsSupportedZeroRPM = 8;
internal const int GetZeroRPMState = 9;
internal const int SetZeroRPMState = 10;
internal const int IsSupportedMinAcousticLimit = 11;
internal const int GetMinAcousticLimitRange = 12;
internal const int GetMinAcousticLimit = 13;
internal const int SetMinAcousticLimit = 14;
internal const int IsSupportedMinFanSpeed = 15;
internal const int GetMinFanSpeedRange = 16;
internal const int GetMinFanSpeed = 17;
internal const int SetMinFanSpeed = 18;
internal const int IsSupportedTargetFanSpeed = 19;
internal const int GetTargetFanSpeedRange = 20;
internal const int GetTargetFanSpeed = 21;
internal const int SetTargetFanSpeed = 22;
}
internal static class IADLXManualGraphicsTuning1 {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int GetGPUTuningRanges = 3;
internal const int GetGPUTuningStates = 4;
internal const int GetEmptyGPUTuningStates = 5;
internal const int IsValidGPUTuningStates = 6;
internal const int SetGPUTuningStates = 7;
}
internal static class IADLXManualGraphicsTuning2 {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int GetGPUMinFrequencyRange = 3;
internal const int GetGPUMinFrequency = 4;
internal const int SetGPUMinFrequency = 5;
internal const int GetGPUMaxFrequencyRange = 6;
internal const int GetGPUMaxFrequency = 7;
internal const int SetGPUMaxFrequency = 8;
internal const int GetGPUVoltageRange = 9;
internal const int GetGPUVoltage = 10;
internal const int SetGPUVoltage = 11;
}
internal static class IADLXManualPowerTuning {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int GetPowerLimitRange = 3;
internal const int GetPowerLimit = 4;
internal const int SetPowerLimit = 5;
internal const int IsSupportedTDCLimit = 6;
internal const int GetTDCLimitRange = 7;
internal const int GetTDCLimit = 8;
internal const int SetTDCLimit = 9;
}
internal static class IADLXManualVRAMTuning1 {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int IsSupportedMemoryTiming = 3;
internal const int GetSupportedMemoryTimingDescriptionList = 4;
internal const int GetMemoryTimingDescription = 5;
internal const int SetMemoryTimingDescription = 6;
internal const int GetVRAMTuningRanges = 7;
internal const int GetVRAMTuningStates = 8;
internal const int GetEmptyVRAMTuningStates = 9;
internal const int IsValidVRAMTuningStates = 10;
internal const int SetVRAMTuningStates = 11;
}
internal static class IADLXManualVRAMTuning2 {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int IsSupportedMemoryTiming = 3;
internal const int GetSupportedMemoryTimingDescriptionList = 4;
internal const int GetMemoryTimingDescription = 5;
internal const int SetMemoryTimingDescription = 6;
internal const int GetMaxVRAMFrequencyRange = 7;
internal const int GetMaxVRAMFrequency = 8;
internal const int SetMaxVRAMFrequency = 9;
}
internal static class IADLXManualTuningState {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int GetFrequency = 3;
internal const int SetFrequency = 4;
internal const int GetVoltage = 5;
internal const int SetVoltage = 6;
}
internal static class IADLXManualTuningStateList {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int Size = 3;
internal const int Empty = 4;
internal const int Begin = 5;
internal const int End = 6;
internal const int At = 7;
internal const int Clear = 8;
internal const int Remove_Back = 9;
internal const int Add_Back = 10;
internal const int At_ManualTuningStateList = 11;
internal const int Add_Back_ManualTuningStateList = 12;
}
internal static class IADLXGPUTuningServices {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int GetGPUTuningChangedHandling = 3;
internal const int IsAtFactory = 4;
internal const int ResetToFactory = 5;
internal const int IsSupportedAutoTuning = 6;
internal const int IsSupportedPresetTuning = 7;
internal const int IsSupportedManualGFXTuning = 8;
internal const int IsSupportedManualVRAMTuning = 9;
internal const int IsSupportedManualFanTuning = 10;
internal const int IsSupportedManualPowerTuning = 11;
internal const int GetAutoTuning = 12;
internal const int GetPresetTuning = 13;
internal const int GetManualGFXTuning = 14;
internal const int GetManualVRAMTuning = 15;
internal const int GetManualFanTuning = 16;
internal const int GetManualPowerTuning = 17;
}
internal static class IADLXGPUMetricsSupport {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int IsSupportedGPUUsage = 3;
internal const int IsSupportedGPUClockSpeed = 4;
internal const int IsSupportedGPUVRAMClockSpeed = 5;
internal const int IsSupportedGPUTemperature = 6;
internal const int IsSupportedGPUHotspotTemperature = 7;
internal const int IsSupportedGPUPower = 8;
internal const int IsSupportedGPUTotalBoardPower = 9;
internal const int IsSupportedGPUFanSpeed = 10;
internal const int IsSupportedGPUVRAM = 11;
internal const int IsSupportedGPUVoltage = 12;
internal const int GetGPUUsageRange = 13;
internal const int GetGPUClockSpeedRange = 14;
internal const int GetGPUVRAMClockSpeedRange = 15;
internal const int GetGPUTemperatureRange = 16;
internal const int GetGPUHotspotTemperatureRange = 17;
internal const int GetGPUPowerRange = 18;
internal const int GetGPUFanSpeedRange = 19;
internal const int GetGPUVRAMRange = 20;
internal const int GetGPUVoltageRange = 21;
internal const int GetGPUTotalBoardPowerRange = 22;
internal const int GetGPUIntakeTemperatureRange = 23;
internal const int IsSupportedGPUIntakeTemperature = 24;
}
internal static class IADLXGPUMetrics {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int TimeStamp = 3;
internal const int GPUUsage = 4;
internal const int GPUClockSpeed = 5;
internal const int GPUVRAMClockSpeed = 6;
internal const int GPUTemperature = 7;
internal const int GPUHotspotTemperature = 8;
internal const int GPUPower = 9;
internal const int GPUTotalBoardPower = 10;
internal const int GPUFanSpeed = 11;
internal const int GPUVRAM = 12;
internal const int GPUVoltage = 13;
internal const int GPUIntakeTemperature = 14;
}
internal static class IADLXPerformanceMonitoringServices {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int GetSamplingIntervalRange = 3;
internal const int SetSamplingInterval = 4;
internal const int GetSamplingInterval = 5;
internal const int GetMaxPerformanceMetricsHistorySizeRange = 6;
internal const int SetMaxPerformanceMetricsHistorySize = 7;
internal const int GetMaxPerformanceMetricsHistorySize = 8;
internal const int ClearPerformanceMetricsHistory = 9;
internal const int GetCurrentPerformanceMetricsHistorySize = 10;
internal const int StartPerformanceMetricsTracking = 11;
internal const int StopPerformanceMetricsTracking = 12;
internal const int GetAllMetricsHistory = 13;
internal const int GetGPUMetricsHistory = 14;
internal const int GetSystemMetricsHistory = 15;
internal const int GetFPSHistory = 16;
internal const int GetCurrentAllMetrics = 17;
internal const int GetCurrentGPUMetrics = 18;
internal const int GetCurrentSystemMetrics = 19;
internal const int GetCurrentFPS = 20;
internal const int GetSupportedGPUMetrics = 21;
internal const int GetSupportedSystemMetrics = 22;
}
internal static class IADLXGPU {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int VendorId = 3;
internal const int ASICFamilyType = 4;
internal const int Type = 5;
internal const int IsExternal = 6;
internal const int Name = 7;
internal const int DriverPath = 8;
internal const int PNPString = 9;
internal const int HasDesktops = 10;
internal const int TotalVRAM = 11;
internal const int VRAMType = 12;
internal const int BIOSInfo = 13;
internal const int DeviceId = 14;
internal const int RevisionId = 15;
internal const int SubSystemId = 16;
internal const int SubSystemVendorId = 17;
internal const int UniqueId = 18;
}
internal static class IADLXGPUList {
internal const int Acquire = 0;
internal const int Release = 1;
internal const int QueryInterface = 2;
internal const int Size = 3;
internal const int Empty = 4;
internal const int Begin = 5;
internal const int End = 6;
internal const int At = 7;
internal const int Clear = 8;
internal const int Remove_Back = 9;
internal const int Add_Back = 10;
internal const int At_GPUList = 11;
internal const int Add_Back_GPUList = 12;
}
internal static class IADLXSystem {
internal const int GetHybridGraphicsType = 0;
internal const int GetGPUs = 1;
internal const int QueryInterface = 2;
internal const int GetDisplaysServices = 3;
internal const int GetDesktopsServices = 4;
internal const int GetGPUsChangedHandling = 5;
internal const int EnableLog = 6;
internal const int Get3DSettingsServices = 7;
internal const int GetGPUTuningServices = 8;
internal const int GetPerformanceMonitoringServices = 9;
internal const int TotalSystemRAM = 10;
internal const int GetI2C = 11;
}
} }
