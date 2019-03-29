using System;
using System.IO.Ports;

namespace Idle.Tools.Instrument
{
    /// <summary>
    /// 串口通信基类
    /// </summary>
    public class SerialPortInstrument:IDisposable
    {
        protected string _endString="\r\n";
        /// <summary>
        /// 同步锁多线程调用
        /// </summary>
        object _objLock = new object();
        /// <summary>
        /// 串口
        /// </summary>
        SerialPort _port;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="portName">com名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="parity">校验</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        public SerialPortInstrument(string portName,int baudRate,Parity parity,int dataBits,StopBits stopBits)
        {
            _port = new SerialPort(portName,baudRate,parity,dataBits,stopBits);
            _port.ReadTimeout = 5000;
            _port.WriteTimeout = 2000;
            _port.Open();
        }

        public SerialPortInstrument(string portName)
        {
            _port = new SerialPort(portName);
            _port.ReadTimeout = 5000;
            _port.WriteTimeout = 2000;
            _port.Open();
        }
        /// <summary>
        /// 释放串口资源
        /// </summary>
        public void Dispose()
        {
            if (_port != null) _port.Close();
        }
        /// <summary>
        /// 向串口发送消息并等待消息返回
        /// </summary>
        /// <param name="msg">发送消息</param>
        /// <returns>返回的消息</returns>
        protected string SendAndRecv(string msg)
        {
            lock(_objLock)
            {
                _port.ReadExisting();
                _port.Write(msg);
                return _port.ReadTo(_endString);
            }
        }
        /// <summary>
        /// 向串口发送消息
        /// </summary>
        /// <param name="msg">待发消息</param>
        protected void SendOnly(string msg)
        {
            lock (_objLock)
            {
                _port.ReadExisting();
                _port.Write(msg);
            }
        }
        /// <summary>
        /// 读取串口消息
        /// </summary>
        /// <returns>接收到的消息</returns>
        protected string RecvOnly()
        {
            lock (_objLock)
            {
                return _port.ReadTo(_endString);
            }
        }
        /// <summary>
        /// 设置握手方式
        /// </summary>
        protected Handshake Handshake { set { _port.Handshake = value; } }
        /// <summary>
        /// 读取串口中剩余数据
        /// </summary>
        /// <returns>数据</returns>
        protected string RecvExisting()
        {
            return _port.ReadExisting();
        }
    }
}
