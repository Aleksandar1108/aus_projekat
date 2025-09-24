using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write coil functions/requests.
    /// </summary>
    public class WriteSingleCoilFunction : ModbusFunction
    {
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            var param = this.CommandParameters as ModbusWriteCommandParameters;

            List<byte> frame = new List<byte>();

            frame.AddRange(BitConverter.GetBytes((ushort)param.TransactionId).Reverse());

            frame.AddRange(new byte[] { 0x00, 0x00 });

            frame.AddRange(BitConverter.GetBytes((ushort)6).Reverse());

            frame.Add(param.UnitId);

            frame.Add((byte)ModbusFunctionCode.WRITE_SINGLE_COIL);

            frame.AddRange(BitConverter.GetBytes((ushort)param.OutputAddress).Reverse());

            ushort coilValue = (param.Value == 0) ? (ushort)0x0000 : (ushort)0xFF00;


            frame.Add((byte)(coilValue >> 8));
            frame.Add((byte)(coilValue & 0xFF));

            return frame.ToArray();
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            ushort address = (ushort)((response[8] << 8) | response[9]);
            ushort value = (ushort)((response[10] << 8) | response[11]);

            ushort coilValue = (value == 0xFF00) ? (ushort)1 : (ushort)0;

            return new Dictionary<Tuple<PointType, ushort>, ushort>
    {
        { new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, address), coilValue }
    };
        }
    }
}
