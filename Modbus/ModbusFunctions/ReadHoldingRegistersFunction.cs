using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read holding registers functions/requests.
    /// </summary>
    public class ReadHoldingRegistersFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadHoldingRegistersFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadHoldingRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            var param = (ModbusReadCommandParameters)CommandParameters;

            List<byte> request = new List<byte>();


            request.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)param.TransactionId)));


            request.AddRange(new byte[] { 0x00, 0x00 });


            request.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)6)));


            request.Add(param.UnitId);


            request.Add(0x03);
            request.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)param.StartAddress)));
            request.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)param.Quantity)));

            return request.ToArray();
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            var result = new Dictionary<Tuple<PointType, ushort>, ushort>();
            var param = (ModbusReadCommandParameters)CommandParameters;


            int byteCount = response[8];
            for (int i = 0; i < byteCount / 2; i++)
            {
                ushort value = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(response, 9 + i * 2));
                result.Add(new Tuple<PointType, ushort>(param.PointType, (ushort)(param.StartAddress + i)), value);
            }

            return result;
        }
    }
}