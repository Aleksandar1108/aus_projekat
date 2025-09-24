using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write single register functions/requests.
    /// </summary>
    public class WriteSingleRegisterFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleRegisterFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleRegisterFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
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

            frame.Add((byte)ModbusFunctionCode.WRITE_SINGLE_REGISTER);

            frame.AddRange(BitConverter.GetBytes((ushort)param.OutputAddress).Reverse());

            frame.AddRange(BitConverter.GetBytes((ushort)param.Value).Reverse());

            return frame.ToArray();
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {

            ushort address = (ushort)((response[8] << 8) | response[9]);
            ushort value = (ushort)((response[10] << 8) | response[11]);

            return new Dictionary<Tuple<PointType, ushort>, ushort>
        {
            { new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, address), value }
        };
        }
    }
}