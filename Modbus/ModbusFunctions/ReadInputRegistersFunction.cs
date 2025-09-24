using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read input registers functions/requests.
    /// </summary>
    public class ReadInputRegistersFunction : ModbusFunction
    {


        private ModbusReadCommandParameters parameters;
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadInputRegistersFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadInputRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
            parameters = (ModbusReadCommandParameters)commandParameters;
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {

            List<byte> pdu = new List<byte>
            {
                (byte)ModbusFunctionCode.READ_INPUT_REGISTERS,
                (byte)(parameters.StartAddress >> 8),
                (byte)(parameters.StartAddress & 0xFF),
                (byte)(parameters.Quantity >> 8),
                (byte)(parameters.Quantity & 0xFF)
            };


            List<byte> adu = new List<byte>();


            adu.Add((byte)(parameters.TransactionId >> 8));
            adu.Add((byte)(parameters.TransactionId & 0xFF));


            adu.Add(0x00);
            adu.Add(0x00);


            adu.Add(0x00);
            adu.Add((byte)(pdu.Count + 1));


            adu.Add(parameters.UnitId);


            adu.AddRange(pdu);

            return adu.ToArray();
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            var result = new Dictionary<Tuple<PointType, ushort>, ushort>();


            byte functionCode = response[7];
            if (functionCode != (byte)ModbusFunctionCode.READ_INPUT_REGISTERS)
                throw new Exception("Invalid function code in response");

            byte byteCount = response[8];
            int numberOfRegisters = byteCount / 2;

            for (int i = 0; i < numberOfRegisters; i++)
            {
                ushort value = (ushort)((response[9 + i * 2] << 8) | response[10 + i * 2]);
                ushort address = (ushort)(parameters.StartAddress + i);

                result.Add(
                    new Tuple<PointType, ushort>(PointType.ANALOG_INPUT, address),
                    value
                );
            }

            return result;
        }
    }
}