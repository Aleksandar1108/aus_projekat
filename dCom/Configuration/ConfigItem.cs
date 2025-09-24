using Common;
using System;
using System.Collections.Generic;

namespace dCom.Configuration
{
    internal class ConfigItem : IConfigItem
    {
        #region Fields

        private PointType registryType;
        private ushort numberOfRegisters;
        private ushort startAddress;
        private ushort decimalSeparatorPlace;
        private ushort minValue;
        private ushort maxValue;
        private ushort defaultValue;
        private string processingType;
        private string description;
        private int acquisitionInterval;
        private double scalingFactor;
        private double deviation;
        private double egu_max;
        private double egu_min;
        private ushort abnormalValue;
        private double highLimit;
        private double lowLimit;
        private int secondsPassedSinceLastPoll;

        #endregion Fields

        #region Properties

        public PointType RegistryType { get => registryType; set => registryType = value; }
        public ushort NumberOfRegisters { get => numberOfRegisters; set => numberOfRegisters = value; }
        public ushort StartAddress { get => startAddress; set => startAddress = value; }
        public ushort DecimalSeparatorPlace { get => decimalSeparatorPlace; set => decimalSeparatorPlace = value; }
        public ushort MinValue { get => minValue; set => minValue = value; }
        public ushort MaxValue { get => maxValue; set => maxValue = value; }
        public ushort DefaultValue { get => defaultValue; set => defaultValue = value; }
        public string ProcessingType { get => processingType; set => processingType = value; }
        public string Description { get => description; set => description = value; }
        public int AcquisitionInterval { get => acquisitionInterval; set => acquisitionInterval = value; }
        public double ScaleFactor { get => scalingFactor; set => scalingFactor = value; }
        public double Deviation { get => deviation; set => deviation = value; }
        public double EGU_Max { get => egu_max; set => egu_max = value; }
        public double EGU_Min { get => egu_min; set => egu_min = value; }
        public ushort AbnormalValue { get => abnormalValue; set => abnormalValue = value; }
        public double HighLimit { get => highLimit; set => highLimit = value; }
        public double LowLimit { get => lowLimit; set => lowLimit = value; }
        public int SecondsPassedSinceLastPoll { get => secondsPassedSinceLastPoll; set => secondsPassedSinceLastPoll = value; }

        #endregion Properties

        public ConfigItem(List<string> configurationParameters)
        {
            int temp;
            double doubleTemp;

            RegistryType = GetRegistryType(configurationParameters[0]);


            Int32.TryParse(configurationParameters[1], out temp);
            NumberOfRegisters = (ushort)temp;

            Int32.TryParse(configurationParameters[2], out temp);
            StartAddress = (ushort)temp;

            Int32.TryParse(configurationParameters[3], out temp);
            DecimalSeparatorPlace = (ushort)temp;

            Int32.TryParse(configurationParameters[4], out temp);
            MinValue = (ushort)temp;

            Int32.TryParse(configurationParameters[5], out temp);
            MaxValue = (ushort)temp;

            Int32.TryParse(configurationParameters[6], out temp);
            DefaultValue = (ushort)temp;


            ProcessingType = configurationParameters[7];


            if (configurationParameters[8].StartsWith("@"))
                Description = configurationParameters[8].Substring(1);
            else
                Description = configurationParameters[8];


            if (configurationParameters[9] == "#")
                AcquisitionInterval = 1;
            else if (Int32.TryParse(configurationParameters[9], out temp))
                AcquisitionInterval = temp;
            else
                AcquisitionInterval = 1;


            ScaleFactor = 1.0;
            Deviation = 0.0;
            EGU_Max = MaxValue;
            EGU_Min = MinValue;
            AbnormalValue = 0;
            HighLimit = double.MaxValue;
            LowLimit = double.MinValue;

            try
            {

                if (configurationParameters.Count > 10 && configurationParameters[10] != "#")
                    double.TryParse(configurationParameters[10], out scalingFactor);


                if (configurationParameters.Count > 11 && configurationParameters[11] != "#")
                    double.TryParse(configurationParameters[11], out deviation);


                if (configurationParameters.Count > 12 && configurationParameters[12] != "#")
                    double.TryParse(configurationParameters[12], out egu_max);


                if (configurationParameters.Count > 13 && configurationParameters[13] != "#")
                    double.TryParse(configurationParameters[13], out egu_min);


                if (configurationParameters.Count > 14 && configurationParameters[14] != "#")
                {
                    Int32.TryParse(configurationParameters[14], out temp);
                    abnormalValue = (ushort)temp;
                }


                if (configurationParameters.Count > 15 && configurationParameters[15] != "#")
                    double.TryParse(configurationParameters[15], out highLimit);


                if (configurationParameters.Count > 16 && configurationParameters[16] != "#")
                    double.TryParse(configurationParameters[16], out lowLimit);


                if (configurationParameters.Count > 17 && configurationParameters[17] != "#")
                {
                    Int32.TryParse(configurationParameters[17], out temp);
                    SecondsPassedSinceLastPoll = temp;
                }
            }
            catch
            {

            }
        }

        private PointType GetRegistryType(string registryTypeName)
        {
            switch (registryTypeName)
            {
                case "DO_REG": return PointType.DIGITAL_OUTPUT;
                case "DI_REG": return PointType.DIGITAL_INPUT;
                case "IN_REG": return PointType.ANALOG_INPUT;
                case "HR_INT": return PointType.ANALOG_OUTPUT;
                default: return PointType.HR_LONG;
            }
        }
    }
}