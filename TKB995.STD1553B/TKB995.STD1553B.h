// TKB995.STD1553B.h

#pragma once
#include <Windows.h>
#include <WinBase.h>
#include "WDMTMKv2.h"
using namespace System;

namespace TKB995STD1553B {
	public enum class  MILSDT_BUS
	{
		A = CX_BUS_A,
		B = CX_BUS_B
	};
	public ref struct PZ_size
	{
	public:
		static unsigned int ncd = 32;
		static unsigned int frame = 2;
	};
	public ref class Manchester
	{
		// TODO: ����� ������� �������� ���� ������ ��� ����� ������.
	private:
		HANDLE _currHandle;
		TTmkEventData *events;
		uint16_t _milstdBus;
		short _word;
		short _addrOY;
		short _currSubAddr;
		short _format;
		short _countOfErrors;
		short _dataInverse;
		short _frame;
		short _errorCode;
		short _mode;
		unsigned short _command;
		int _setPlate;
		unsigned int _wBase;
		bool _captured;
		unsigned long  wStatus;
		unsigned short wBase, wMaxBase, wSubAddr, wLen, wState;
		unsigned long dwGoodStarts, dwBusyStarts, dwErrStarts, dwStatStarts;
		unsigned long dwStarts;
		unsigned short *_sendingMass;
		unsigned short *_receivedData;
		unsigned short **_pz;
		
		/// <summary> �����, ����������� �������/���������
		/// </summary>
		/// <param name="K">����������� �������� (� = 0 - �������� ������, � = 1 - ���������).</param>
		short sendingControl(short K);

		/// <summary> �����, ����������� ��������� �����
		/// </summary>
		/// <param name="addrOy">������� ����� ���������� ����������.</param>
		/// <param name="K">����������� �������� (� = 0 - �������� ������, � = 1 - ���������).</param>
		/// <param name="subAddr">������� ��������.</param>
		/// <param name="ncd">���������� ������������ ���� ������ (�� ����� 32).</param>
		short formatKC(short addrOy, short K, short subAddr, short ncd);


		/// <summary> ����� ��������� ���� ������
		/// </summary>
		/// <param name="oc">�������� ����� �� ���������� ����������.</param>
		void errorParse(short oc);
	public:
		/// <summary>����������� ������ ������ � ����������� MILSTD1553.
		/// </summary>
		Manchester();
		/// <summary>���������� ������ ������ � ����������� MILSTD1553.
		/// </summary>
		~Manchester();

		/// <summary>�������� ��������� ������������ ���� ���������� MILSTD1553 (A ��� B)
		/// </summary>
		property MILSDT_BUS SetMilstdBus
		{
			virtual void set(MILSDT_BUS bus)
			{
				this->_milstdBus = (uint16_t)bus;
			}
		}

		/// <summary>�������� ��������� ������� ���� ���������� MILSTD1553 (A ��� B)
		/// </summary>
		property MILSDT_BUS GetMilstdBus
		{
			virtual MILSDT_BUS get()
			{
				return (MILSDT_BUS)this->_milstdBus;
			}
		}

		/// <summary>��������� � ��������� ������ �� ������ ��
		/// </summary>
		property unsigned short **PZ{
			virtual unsigned short ** get(){
				return this->_pz;
			}
			virtual void set(unsigned short ** newPZ){
				this->_pz = newPZ;
			}
		}

		/// <summary>��������� ������� �������� ������ �� ��
		/// </summary>
		property unsigned short* Received{
			virtual unsigned short* get(){
				return this->_receivedData;
			}
		}

		/// <summary>��������� � ��������� �������� ������
		/// </summary>
		property short Mode{
			virtual short get(){
				return this->_mode;
			}
			virtual void set(short newMode){
				this->_mode = newMode;
			}
		}

		/// <summary>��������� �������� ���������� �����
		/// </summary>
		property unsigned short CurrentCommandWord{
			virtual unsigned short get(){
				return _command;
			}
		}

		/// <summary>��������� ���� ������
		/// </summary>
		property short OCErr{ 
			virtual short get(){
				return _errorCode;
			}
		}

		/// <summary>��������� � ��������� �������� HANDLE
		/// </summary>
		property HANDLE CurrentHandle{ 
			virtual HANDLE get(){
				return _currHandle;
			}
			virtual void set(HANDLE value){
				_currHandle = value;
			}
		}

		/// <summary>��������� �������� ������
		/// </summary>
		property short Frame { 
			virtual short get(){
				return _frame;
			}
			virtual void set(short value){
				_frame = value;
			}
		}

		/// <summary>��������� �������� ���������
		/// </summary>
		property short GetSubAddress{
			virtual short get(){
				return this->_currSubAddr;
			}
		}

		/// <summary>�����, ��������������� ������ � �������� � 1 ����
		/// </summary>
		/// <param name="data">���� ������ (�������� � ������� ����� �����).</param>
		virtual short inverseData(short data);
 
		/// <summary>�����, ���������� ���������� ����� �� 2 ����.
		/// </summary>
		/// <param name="lowData">������� ���� (�������� � ������� ����� �����).</param>
		/// <param name="highData">>������� ���� (�������� � ������� ����� �����).</param>
		virtual short ConcatinateData(short lowData, short highData);

		/// <summary>�����, ������������ ������ ��������� ����� ����������.
		/// </summary>
		/// <param name="currentPlate">������� ����� �����.</param>
		virtual int configuration(int currentPlate);

		/// <summary>�����, ������������ ������������ �����.
		/// </summary>
		/// <param name="currentPlate">������� ����� �����.</param>
		virtual bool toFreePlate(int currentPlate);

		/// <summary>�����, ������������ �������� ������ �� ��.
		/// </summary>
		/// <param name="oldAddr">������ ����� ���������� ����������.</param>
		/// <param name="addrOy">������� ����� ���������� ����������.</param>
		/// <param name="K">����������� �������� (� = 0 - �������� ������, � = 1 - ���������).</param>
		/// <param name="subAddr">������� ��������.</param>
		/// <param name="ncd">���������� ������������ ���� ������ (�� ����� 32).</param>
		/// <param name="massToSend">��������� �� ������ ������. ������ uint16.</param>
		virtual bool sendData(short oldAddr, short addrOy, short K, short subAddr, short ncd, unsigned short * massToSend);

		/// <summary>������������ ���������� �� ��������� ������.
		/// </summary>
		/// <param name="wCtrlCode">Used to indicate status.</param>
		virtual int WaitInt(unsigned short wCtrlCode);
	};
}
