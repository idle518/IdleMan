using System.Text;
using System.IO.Ports;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Idle.Tools.Instrument
{
    /// <summary>
    /// use host link protocal you need to change plc mode to monitor mode.
    /// </summary>
    /// 
    public class OmronPlc:SerialPortInstrument
    {
        public enum Area{ X=0X30,Y=0x30,WR=0X31,HR=0X32,AR=0X33,DM=0X02,NONE=-1};
    	public enum MODE{BIT=0,WORD=0x80};
    	
        const string _cmdHeader="@00FA00000000001";

        public OmronPlc(string portName)
            : base(portName, 9600, Parity.Even, 7, StopBits.Two)
        {
            _endString = "*\r";
        }
        
        public void SetValue(string device,int state)
        {
            Match m = Regex.Match(device.ToUpper(), @"^[XYWHAD][0-9]{1,4}\.{0,1}[0-9]{1,2}");
            if (m.Length == 0) throw new FormatException("param device format error.");
            Area area;
            int startAddr;
            MODE mode;
        	DecodeDevice(m.Value,out area,out mode,out startAddr);
            if ((area<0) || (startAddr < 0)) return;
        	SetValue(area,mode,startAddr,state);
        }

        void DecodeDevice(string device, out Area area, out MODE bitOrWord, out int startAddr)
        {
            area = (Area)Enum.Parse(typeof(Area), device.Substring(0,1));
            int index = device.IndexOf('.');
            int high = 0;
            int low = 0;
            if (index > 0)
            {
                low = Convert.ToInt32(device.Substring(index + 1));
                high = Convert.ToInt32(device.Substring(1, index - 1));
                bitOrWord = MODE.BIT;
            }
            else
            {
                high = Convert.ToInt32(device.Substring(1,device.Length-1));
                bitOrWord = MODE.WORD;
            }
            startAddr = (high << 8) | low;
        }
        
        public void SetValue(Area area,MODE mode,int startAddr,int state)
        {
        	int[] states=new int[]{state};
        	SetValue(area,mode,startAddr,states);
        }
        
        public void SetValue(Area area,MODE mode,int startAddr,int[] states)
        {
        	StringBuilder cmd=new StringBuilder(512);
        	cmd.Append(_cmdHeader);
        	cmd.AppendFormat("02{0:X2}{1:X6}{2:X4}",(int)area+(int)mode,startAddr,states.Length);
        	if(MODE.BIT==mode){
	        	foreach(int state in states){
	        		cmd.Append(state==0?"00":"01");
	        	}
        	}else{
        		foreach(int state in states){
        			cmd.Append(state.ToString("X4"));
        		}
        	}
        	WritePort(cmd);
        }
        
        public int GetValue(string device){
            Match m = Regex.Match(device.ToUpper(), @"^[XYWHAD][0-9]{1,4}\.{0,1}[0-9]{1,2}");
            if (m.Length == 0) throw new FormatException("param device format error.") ;
            Area area;
            int startAddr;
            MODE mode;
            DecodeDevice(m.Value, out area, out mode, out startAddr);
        	int[] ret = GetValue(area,mode,startAddr,1);
        	return ret[0];
        }
        
        public int[] GetValue(Area area,MODE mode,int startAddr,int count){
        	int[] results=new int[count];
        	int len=2;
        	
        	if(MODE.WORD==mode)len=4;
        	StringBuilder cmd=new StringBuilder(512);
        	cmd.Append(_cmdHeader);
        	cmd.AppendFormat("01{0:X2}{1:X6}{2:X4}",(int)area+(int)mode,startAddr,count);
        	
        	string ret=WritePort(cmd);
        	if(null!=ret){
	        	for(int i=0;i<count;i++){
	        		results[i]=Convert.ToInt32(ret.Substring(23+i*len,len),16);
	        	}
        	}
        	return results;
        }

        public int GetValue(Area area, MODE mode, int startAddr)
        {
            int[] ret = GetValue(area, mode, startAddr, 1);
            return ret[0];
        }

        void CheckFCS(StringBuilder cmd){
        	int sum=0;
        	for(int i=0;i<cmd.Length;i++){
        		sum^=(int)cmd[i];
        	}
        	
        	cmd.AppendFormat("{0:X2}*\r",sum&0xff);
        }

        string WritePort(StringBuilder cmd)
        {
            string ret = null;
            CheckFCS(cmd);
            ret = SendAndRecv(cmd.ToString());
            if ("00" != ret.Substring(21, 2)) { throw new Exception("OMRON plc 通信错误,请检查串口是否配置正确:"+ret); }

            return ret;
        }
    }
}
