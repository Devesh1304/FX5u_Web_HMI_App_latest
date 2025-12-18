using FX5u_Web_HMI_App;
using HslCommunication;
using HslCommunication.Profinet.Melsec;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

public class SLMPService : ISLMPService, IDisposable
{
    private readonly string _ipAddress;
    private readonly int _port;
    private readonly string _heartbeatAddress; // <-- ADDED
    private readonly ILogger<SLMPService> _logger;
    private readonly object _lock = new object();
    private MelsecMcNet? _slmpClient;
    private Timer? _heartbeat;
    private short _currentHeartbeatValue;

    public SLMPService(IConfiguration config, ILogger<SLMPService> logger)
    {
        _logger = logger;

        // --- UPDATED CONFIGURATION LOADING ---
        _ipAddress = config.GetValue<string>("PLCSettings:IpAddress") ?? "192.168.0.10";
        _port = config.GetValue<int?>("PLCSettings:Port") ?? 5002; // Simplified port loading
        _heartbeatAddress = config.GetValue<string>("PLCSettings:HeartbeatAddress") ?? "M0";
        var heartbeatInterval = config.GetValue<int?>("PLCSettings:HeartbeatIntervalMs") ?? 5000;
        // ------------------------------------

        EnsureConnected(); // Initial connection attempt

        // Start a background timer using the configured interval
        _heartbeat = new Timer(_ => HeartbeatCheck(), null, heartbeatInterval, heartbeatInterval);
    }

    private void EnsureConnected()
    {
        lock (_lock)
        {
            if (_slmpClient == null)
            {
                _logger.LogInformation("Attempting to connect to PLC at {ip}:{port}", _ipAddress, _port);
                _slmpClient = new MelsecMcNet(_ipAddress, _port) { 
                    ConnectTimeOut = 500,
                    ReceiveTimeOut = 2000
                };

                var result = _slmpClient.ConnectServer();
                if (result.IsSuccess)
                {
                    _logger.LogInformation("PLC connected successfully.");
                }
                else
                {
                    _logger.LogError("PLC connection failed: {msg}", result.Message);
                    _slmpClient = null;
                }
            }
        }
    }

    private void HeartbeatCheck()
    {
        if (_slmpClient == null)
        {
            _logger.LogWarning("Heartbeat check failed: Client is null. Forcing reconnect attempt.");
            EnsureConnected();
            return;
        }

        var readResult = _slmpClient.ReadBool(_heartbeatAddress, 1);
        if (!readResult.IsSuccess)
        {
            _logger.LogWarning("Heartbeat read failed, reconnecting... Reason: {msg}", readResult.Message);
            var oldClient = _slmpClient;
            _slmpClient = null;
            Task.Run(() => oldClient?.ConnectClose());
            return;
        }

        // --- RECURRING WRITE LOGIC ---
        try
        {
            if (_currentHeartbeatValue != 0)
            {
                var writeResult = _slmpClient.Write("D0", _currentHeartbeatValue);
                if (!writeResult.IsSuccess)
                {
                    _logger.LogWarning("Recurring write to D0 failed: {msg}", writeResult.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred during the recurring write to D0.");
        }
    }

    public async Task<OperateResult<short>> ReadInt16Async(string address)
    {
        return await Task.Run(() =>
        {
            EnsureConnected();
            return _slmpClient?.ReadInt16(address) ?? new OperateResult<short>("Client not connected.");
        });
    }

    public async Task<OperateResult<int>> ReadInt32Async(string address)
    {
        return await Task.Run(() =>
        {
            EnsureConnected();
            // This calls the HslCommunication library's method to read a 32-bit integer
            return _slmpClient?.ReadInt32(address) ?? new OperateResult<int>("Client not connected.");
        });
    }
    public async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
    {
        return await Task.Run(() =>
        {
            EnsureConnected();
            return _slmpClient?.ReadBool(address, length) ?? new OperateResult<bool[]>("Client not connected.");
        });
    }

    public async Task<OperateResult<float>> ReadFloatAsync(string address)
    {
        return await Task.Run(() =>
        {
            EnsureConnected();
            return _slmpClient?.ReadFloat(address) ?? new OperateResult<float>("Client not connected.");
        });
    }

    public async Task<OperateResult> WriteAsync(string address, short value)
    {
        return await Task.Run(() =>
        {
            EnsureConnected();
            return _slmpClient?.Write(address, value) ?? new OperateResult("Client not connected.");
        });
    }

    public async Task<OperateResult> WriteAsync(string address, float value)
    {
        return await Task.Run(() =>
        {
            EnsureConnected();
            return _slmpClient?.Write(address, value) ?? new OperateResult("Client not connected.");
        });
    }

    // Note: This is a "read-modify-write" operation and is not atomic.
    // If two web requests call this at the same time for the same address, a race condition can occur.
    // For critical operations, implement toggle logic inside the PLC program itself.
    public async Task<OperateResult> WriteBoolAsync(string address, bool value)
    {
        return await Task.Run(() =>
        {
            EnsureConnected();
            // This calls the HslCommunication library's method to write a single boolean value
            return _slmpClient?.Write(address, value) ?? new OperateResult("Client not connected.");
        });
    }

    public async Task<OperateResult> ToggleBitAsync(string address, ushort bitPosition)
    {
        return await Task.Run(() =>
        {
            EnsureConnected();
            if (_slmpClient == null) return new OperateResult("Client not connected.");

            // Assuming we are toggling a bit within a 16-bit word (like D-register)
            OperateResult<short> readResult = _slmpClient.ReadInt16(address);
            if (!readResult.IsSuccess) return readResult;

            short currentValue = readResult.Content;
            // Toggle the specified bit using bitwise XOR
            short newValue = (short)(currentValue ^ (1 << bitPosition));

            return _slmpClient.Write(address, newValue);
        });
    }

    public async Task<OperateResult<short[]>> ReadInt16BlockAsync(string startAddress, ushort length)
    {
        return await Task.Run(() =>
        {
            EnsureConnected();
            // This calls the HslCommunication library's method to read a block of short values
            return _slmpClient?.ReadInt16(startAddress, length) ?? new OperateResult<short[]>("Client not connected.");
        });
    }

    public async Task<OperateResult> WriteInt32Async(string address, int value)
    {
        return await Task.Run(() =>
        {
            EnsureConnected();
            return _slmpClient?.Write(address, value) ?? new OperateResult("Client not connected.");
        });
    }

    public async Task<OperateResult<string>> ReadStringAsync(string address, ushort length)
    {
        return await Task.Run(() =>
        {
            EnsureConnected();
            // This calls the HslCommunication library's method to read a string
            // It reads 'length' bytes and decodes them as a string.
            return _slmpClient?.ReadString(address, length) ?? new OperateResult<string>("Client not connected.");
        });
    }

    // --- ADD THIS METHOD FOR WRITING STRINGS ---
    public async Task<OperateResult> WriteStringAsync(string address, string value)
    {
        return await Task.Run(() =>
        {
            EnsureConnected();
            // This calls the HslCommunication library's method to write a string.
            // The library handles encoding the string to bytes.
            return _slmpClient?.Write(address, value) ?? new OperateResult("Client not connected.");
        });
    }
    public void SetHeartbeatValue(short value)
    {
        _logger.LogInformation("Setting recurring heartbeat value to {value}", value);
        _currentHeartbeatValue = value;
    }

    public void Disconnect()
    {
        _logger.LogInformation("Application is shutting down. Disconnecting SLMP client.");
        _slmpClient?.ConnectClose();
    }

    public void Dispose()
    {
        _heartbeat?.Dispose();
        _slmpClient?.ConnectClose();
    }
}