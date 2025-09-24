using Common;
using Modbus;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for processing points and executing commands.
    /// </summary>
    public class ProcessingManager : IProcessingManager
    {
        private IFunctionExecutor functionExecutor;
        private IStorage storage;
        private AlarmProcessor alarmProcessor;
        private EGUConverter eguConverter;

        public ProcessingManager(IStorage storage, IFunctionExecutor functionExecutor)
        {
            this.storage = storage;
            this.functionExecutor = functionExecutor;
            this.alarmProcessor = new AlarmProcessor();
            this.eguConverter = new EGUConverter();
            this.functionExecutor.UpdatePointEvent += CommandExecutor_UpdatePointEvent;
        }

        public void ExecuteReadCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort startAddress, ushort numberOfPoints)
        {
            ModbusReadCommandParameters p = new ModbusReadCommandParameters(
                6,
                (byte)GetReadFunctionCode(configItem.RegistryType),
                startAddress,
                numberOfPoints,
                transactionId,
                remoteUnitAddress
            );
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }

        public void ExecuteWriteCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            if (configItem.RegistryType == PointType.DIGITAL_OUTPUT)
            {

                ushort coilValue = (value == 0) ? (ushort)0 : (ushort)1;

                var pts = storage.GetPoints(new List<PointIdentifier> {
                    new PointIdentifier(PointType.DIGITAL_OUTPUT, pointAddress)
                });
                if (pts.Count > 0)
                {
                    var dp = pts[0] as IDigitalPoint;
                    if (dp != null)
                    {
                        dp.State = (DState)coilValue;
                        dp.RawValue = coilValue;
                        dp.Timestamp = DateTime.Now;

                        AlarmType alarm = alarmProcessor.GetAlarmForDigitalPoint(coilValue, configItem);
                        dp.Alarm = alarm;
                    }
                }


                ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, pointAddress, coilValue);
            }
            else if (configItem.RegistryType == PointType.ANALOG_OUTPUT)
            {
                var pts = storage.GetPoints(new List<PointIdentifier> {
                    new PointIdentifier(PointType.ANALOG_OUTPUT, pointAddress)
                });
                if (pts.Count > 0)
                {
                    var ap = pts[0] as IAnalogPoint;
                    if (ap != null)
                    {
                        ap.RawValue = (ushort)value;
                        double egu = eguConverter.ConvertToEGU(configItem.ScaleFactor, configItem.Deviation, (ushort)value);
                        ap.EguValue = egu;
                        ap.Timestamp = DateTime.Now;

                        AlarmType alarm = alarmProcessor.GetAlarmForAnalogPoint(egu, configItem);
                        ap.Alarm = alarm;
                    }
                }

                ExecuteAnalogCommand(configItem, transactionId, remoteUnitAddress, pointAddress, value);
            }
        }

        private void ExecuteDigitalCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {

            ModbusWriteCommandParameters p = new ModbusWriteCommandParameters(
                6,
                (byte)ModbusFunctionCode.WRITE_SINGLE_COIL,
                pointAddress,
                (ushort)value,
                transactionId,
                remoteUnitAddress
            );
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }

        private void ExecuteAnalogCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            ModbusWriteCommandParameters p = new ModbusWriteCommandParameters(
                6,
                (byte)ModbusFunctionCode.WRITE_SINGLE_REGISTER,
                pointAddress,
                (ushort)value,
                transactionId,
                remoteUnitAddress
            );
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }

        private ModbusFunctionCode? GetReadFunctionCode(PointType registryType)
        {
            switch (registryType)
            {
                case PointType.DIGITAL_OUTPUT: return ModbusFunctionCode.READ_COILS;
                case PointType.DIGITAL_INPUT: return ModbusFunctionCode.READ_DISCRETE_INPUTS;
                case PointType.ANALOG_INPUT: return ModbusFunctionCode.READ_INPUT_REGISTERS;
                case PointType.ANALOG_OUTPUT: return ModbusFunctionCode.READ_HOLDING_REGISTERS;
                case PointType.HR_LONG: return ModbusFunctionCode.READ_HOLDING_REGISTERS;
                default: return null;
            }
        }

        private void CommandExecutor_UpdatePointEvent(PointType type, ushort pointAddress, ushort newValue)
        {
            List<IPoint> points = storage.GetPoints(new List<PointIdentifier>(1) { new PointIdentifier(type, pointAddress) });

            if (type == PointType.ANALOG_INPUT || type == PointType.ANALOG_OUTPUT)
            {
                ProcessAnalogPoint(points.First() as IAnalogPoint, newValue);
            }
            else
            {
                ProcessDigitalPoint(points.First() as IDigitalPoint, newValue);
            }
        }

        private void ProcessDigitalPoint(IDigitalPoint point, ushort newValue)
        {
            point.RawValue = newValue;
            point.Timestamp = DateTime.Now;
            point.State = (DState)newValue;

            AlarmType alarm = alarmProcessor.GetAlarmForDigitalPoint(newValue, point.ConfigItem);
            point.Alarm = alarm;
        }

        private void ProcessAnalogPoint(IAnalogPoint point, ushort newValue)
        {
            point.RawValue = newValue;
            point.Timestamp = DateTime.Now;

            double egu = eguConverter.ConvertToEGU(point.ConfigItem.ScaleFactor, point.ConfigItem.Deviation, newValue);
            if (egu > point.ConfigItem.EGU_Max) egu = point.ConfigItem.EGU_Max;
            if (egu < point.ConfigItem.EGU_Min) egu = point.ConfigItem.EGU_Min;
            point.EguValue = egu;

            AlarmType alarm = alarmProcessor.GetAlarmForAnalogPoint(egu, point.ConfigItem);
            point.Alarm = alarm;
        }

        public void InitializePoint(PointType type, ushort pointAddress, ushort defaultValue)
        {
            List<IPoint> points = storage.GetPoints(new List<PointIdentifier>(1) { new PointIdentifier(type, pointAddress) });

            if (type == PointType.ANALOG_INPUT || type == PointType.ANALOG_OUTPUT)
            {
                ProcessAnalogPoint(points.First() as IAnalogPoint, defaultValue);
            }
            else
            {
                ProcessDigitalPoint(points.First() as IDigitalPoint, defaultValue);
            }
        }
    }
}