using HslCommunication;
using System.Threading.Tasks;

namespace FX5u_Web_HMI_App
{
    public interface ISLMPService
    {
        Task<OperateResult<short>> ReadInt16Async(string address);
        Task<OperateResult<int>> ReadInt32Async(string address);
        Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length);
        Task<OperateResult<float>> ReadFloatAsync(string address);
        Task<OperateResult> WriteAsync(string address, short value);
        Task<OperateResult> WriteInt32Async(string address, int value);
        Task<OperateResult> WriteAsync(string address, float value);
        Task<OperateResult> WriteBoolAsync(string address, bool value);
        Task<OperateResult> ToggleBitAsync(string address, ushort bitPosition);
        Task<OperateResult<short[]>> ReadInt16BlockAsync(string startAddress, ushort length);
        Task<OperateResult<string>> ReadStringAsync(string address, ushort length);        Task<OperateResult> WriteStringAsync(string address, string value);

        void SetHeartbeatValue(short value);

        void Disconnect();
    }

}
