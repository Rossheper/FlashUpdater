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
		// TODO: здесь следует добавить свои методы для этого класса.
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
		
		/// <summary> Метод, управляющий приемом/передачей
		/// </summary>
		/// <param name="K">Направление передачи (к = 0 - передача данных, к = 1 - получение).</param>
		short sendingControl(short K);

		/// <summary> Метод, формирующий командное слово
		/// </summary>
		/// <param name="addrOy">Текущий адрес оконечного устройства.</param>
		/// <param name="K">Направление передачи (к = 0 - передача данных, к = 1 - получение).</param>
		/// <param name="subAddr">Текущий подадрес.</param>
		/// <param name="ncd">Количество передаваемых слов данных (не более 32).</param>
		short formatKC(short addrOy, short K, short subAddr, short ncd);


		/// <summary> Метод генерации кода ошибок
		/// </summary>
		/// <param name="oc">Ответное слово от оконечного устройства.</param>
		void errorParse(short oc);
	public:
		/// <summary>Конструктор класса работы с интерфейсом MILSTD1553.
		/// </summary>
		Manchester();
		/// <summary>Деструктор класса работы с интерфейсом MILSTD1553.
		/// </summary>
		~Manchester();

		/// <summary>Свойства установки используемой шины интерфейса MILSTD1553 (A или B)
		/// </summary>
		property MILSDT_BUS SetMilstdBus
		{
			virtual void set(MILSDT_BUS bus)
			{
				this->_milstdBus = (uint16_t)bus;
			}
		}

		/// <summary>Свойства получения текущей шины интерфейса MILSTD1553 (A или B)
		/// </summary>
		property MILSDT_BUS GetMilstdBus
		{
			virtual MILSDT_BUS get()
			{
				return (MILSDT_BUS)this->_milstdBus;
			}
		}

		/// <summary>Получение и установка ссылки на массив ПЗ
		/// </summary>
		property unsigned short **PZ{
			virtual unsigned short ** get(){
				return this->_pz;
			}
			virtual void set(unsigned short ** newPZ){
				this->_pz = newPZ;
			}
		}

		/// <summary>Получение массива притяных данных от ОУ
		/// </summary>
		property unsigned short* Received{
			virtual unsigned short* get(){
				return this->_receivedData;
			}
		}

		/// <summary>Получение и установка текущего режима
		/// </summary>
		property short Mode{
			virtual short get(){
				return this->_mode;
			}
			virtual void set(short newMode){
				this->_mode = newMode;
			}
		}

		/// <summary>Получение текущего командного слова
		/// </summary>
		property unsigned short CurrentCommandWord{
			virtual unsigned short get(){
				return _command;
			}
		}

		/// <summary>Получение кода ошибки
		/// </summary>
		property short OCErr{ 
			virtual short get(){
				return _errorCode;
			}
		}

		/// <summary>Получение и установка текущего HANDLE
		/// </summary>
		property HANDLE CurrentHandle{ 
			virtual HANDLE get(){
				return _currHandle;
			}
			virtual void set(HANDLE value){
				_currHandle = value;
			}
		}

		/// <summary>Получение текущего фрейма
		/// </summary>
		property short Frame { 
			virtual short get(){
				return _frame;
			}
			virtual void set(short value){
				_frame = value;
			}
		}

		/// <summary>Получение текущего подадреса
		/// </summary>
		property short GetSubAddress{
			virtual short get(){
				return this->_currSubAddr;
			}
		}

		/// <summary>Метод, разворачивающий данные с размером в 1 байт
		/// </summary>
		/// <param name="data">Байт данных (хранится в младшем байте слова).</param>
		virtual short inverseData(short data);
 
		/// <summary>Метод, собирающий полученное слово из 2 байт.
		/// </summary>
		/// <param name="lowData">Младший байт (хранится в младшем байте слова).</param>
		/// <param name="highData">>Старший байт (хранится в младшем байте слова).</param>
		virtual short ConcatinateData(short lowData, short highData);

		/// <summary>Метод, производящий захват выбранной платы манчестера.
		/// </summary>
		/// <param name="currentPlate">Текущий номер платы.</param>
		virtual int configuration(int currentPlate);

		/// <summary>Метод, производящий освобождение платы.
		/// </summary>
		/// <param name="currentPlate">Текущий номер платы.</param>
		virtual bool toFreePlate(int currentPlate);

		/// <summary>Метод, производящий отправку данных на ОУ.
		/// </summary>
		/// <param name="oldAddr">Старый адрес оконечного устройства.</param>
		/// <param name="addrOy">Текущий адрес оконечного устройства.</param>
		/// <param name="K">Направление передачи (к = 0 - передача данных, к = 1 - получение).</param>
		/// <param name="subAddr">Текущий подадрес.</param>
		/// <param name="ncd">Количество передаваемых слов данных (не более 32).</param>
		/// <param name="massToSend">Указатель на массив данных. Формат uint16.</param>
		virtual bool sendData(short oldAddr, short addrOy, short K, short subAddr, short ncd, unsigned short * massToSend);

		/// <summary>Обрабатывает прерывание на получение данных.
		/// </summary>
		/// <param name="wCtrlCode">Used to indicate status.</param>
		virtual int WaitInt(unsigned short wCtrlCode);
	};
}
