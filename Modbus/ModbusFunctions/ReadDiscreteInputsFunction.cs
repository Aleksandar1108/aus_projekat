using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read discrete inputs functions/requests.
    /// </summary>
    public class ReadDiscreteInputsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadDiscreteInputsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadDiscreteInputsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            var parameters = this.CommandParameters as ModbusReadCommandParameters;

            List<byte> pdu = new List<byte>
    {
        (byte)ModbusFunctionCode.READ_DISCRETE_INPUTS,
        (byte)(parameters.StartAddress >> 8),
        (byte)(parameters.StartAddress & 0xFF),
        (byte)(parameters.Quantity >> 8),
        (byte)(parameters.Quantity & 0xFF)
    };

            List<byte> adu = new List<byte>
    {
        (byte)(parameters.TransactionId >> 8),
        (byte)(parameters.TransactionId & 0xFF),
        0x00, 0x00,
        0x00, (byte)(pdu.Count + 1),
        parameters.UnitId
    };

            adu.AddRange(pdu);
            return adu.ToArray();
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            Dictionary<Tuple<PointType, ushort>, ushort> result = new Dictionary<Tuple<PointType, ushort>, ushort>();
            var parameters = this.CommandParameters as ModbusReadCommandParameters;

            byte functionCode = response[7];
            if (functionCode != (byte)ModbusFunctionCode.READ_DISCRETE_INPUTS)
                throw new Exception("Invalid function code in response!");

            byte byteCount = response[8];
            ushort startAddress = parameters.StartAddress;

            for (int i = 0; i < byteCount; i++)
            {
                byte currentByte = response[9 + i];
                for (int bit = 0; bit < 8; bit++)
                {
                    ushort value = (ushort)((currentByte >> bit) & 0x01);
                    result.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_INPUT, (ushort)(startAddress++)), value);
                    if (startAddress >= parameters.StartAddress + parameters.Quantity)
                        break;
                }
            }

            return result;
        }
    }
}