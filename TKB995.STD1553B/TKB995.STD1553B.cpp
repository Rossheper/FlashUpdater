// Главный DLL-файл.

#include "stdafx.h"
#include "TKB995.STD1553B.h"
using namespace TKB995STD1553B;

Manchester::Manchester() {
	this->_milstdBus = CX_BUS_A; // По-умолчанию шина А
	this->_word = 0;
	this->_dataInverse = 0;
	this->_command = 0;
	this->_setPlate = 0;
	this->_currHandle = 0;
	this->_wBase = 0;
	this->_captured = false;
	this->_receivedData = new unsigned short[32];
	this->_sendingMass = new unsigned short[32];
	this->_addrOY = 0;
	this->_currSubAddr = 0;
	this->_format = 0;
	this->_countOfErrors = 0;
	this->_frame = 0;
	this->_errorCode = 0;
	this->wBase = 0;
	this->wMaxBase = 0;
	this->wSubAddr = 0;
	this->wLen = 0;
	this->wState = 0;
	this->_mode = 0;
	this->wStatus = 0;
	this->dwGoodStarts = 0;
	this->dwBusyStarts = 0;
	this->dwErrStarts = 0;
	this->dwStatStarts = 0;
	this->dwStarts = 0;
	this->events = new TTmkEventData();
	
	_pz = new unsigned short *[PZ_size::frame];
	for (int i = 0; i < PZ_size::frame; i++) {
		_pz[i] = new unsigned short[PZ_size::ncd];
	}
	for (int i = 0; i < PZ_size::ncd; i++) {
		_sendingMass[i] = 0;
	}
}

Manchester::~Manchester() {
	if (this->_pz != nullptr) {
		for (int i = 0; i < PZ_size::frame; i++) {
			delete[] this->_pz[i];
		}
		delete[]this->_pz;
	}
	delete[]this->_receivedData;
	delete[]this->_sendingMass;
}

int Manchester::WaitInt(unsigned short wCtrlCode) {
	switch (WaitForSingleObjectEx(this->_currHandle, 1000,true))
	{
	case WAIT_OBJECT_0:
		ResetEvent(this->_currHandle);
		break;
	case WAIT_TIMEOUT:
		return 1;
	default:
		return 1;
	}
	tmkgetevd(events);

	if (events->bcx.wResultX & SX_IB_MASK) {
		if (((events->bcx.wResultX & SX_ERR_MASK) == SX_NOERR) || ((events->bcx.wResultX & SX_ERR_MASK) == SX_TOD)) {
			wStatus = bcgetansw(wCtrlCode);
			if (wStatus & BUSY_MASK)
				++dwBusyStarts;
			else
				++dwStatStarts;

		}
		else {
			++dwErrStarts;
		}
	}
	else if (events->bcx.wResultX & SX_ERR_MASK) {
		++dwErrStarts;
	}
	else {
		++dwGoodStarts;
	}
	++dwStarts;
	return 0;
}

// Метод, контролирующий режим передачи
short Manchester::sendingControl(short K) {
	if (K == 1)
		return DATA_RT_BC;
	return DATA_BC_RT;
}

// Метод, формирующий командное слово
short Manchester::formatKC(short addrOy, short K, short subAddr, short ncd) {
	this->_command = 0;

	// Установили адрес и сдвинули на 1 разряд влево
	// для установки флага К (прием/передача)
	this->_command = (this->_command + addrOy) << 1;

	// Установили флаг К и сдвинули на 5 разрядов влево
	// для установки подадреса
	this->_command = (this->_command + K) << 5;

	// Установили подадрес и сдвинули на 5 разрядов влево
	// для установки количества следующих СД (слов данных)
	this->_command = (this->_command + subAddr) << 5;

	// Установили количество СД
	this->_command = (this->_command + (ncd & 0x1F));

	return this->_command;
}

// Метод, разворачивающий данные с размером 1 байт
short Manchester::inverseData(short data) {
	for (int i = 0; i < 7; i++) {
		if (((data >> i) & 0x01) == 1)
			this->_dataInverse += 1;
		this->_dataInverse = this->_dataInverse << 1;
	}
	return this->_dataInverse;
}

// Метод, собирающий полученное слово из 2 байт
short Manchester::ConcatinateData(short lowData, short highData) {
	this->_word = highData;
	return this->_word = (this->_word << 8) + lowData;
}

// Метод, производящий захват выбранной платы манчестера.
int Manchester::configuration(int currentPlate) {

	TmkOpen();

	//Сконфигурировали
	if (tmkconfig(currentPlate) != (int)TMK_BAD_NUMBER) {

		//Захватили плату
		if (tmkselect(currentPlate) == (int)TMK_BAD_NUMBER)
			return -1;

		//Запомнили событие и номер платы
		this->_setPlate = currentPlate;
		ResetEvent(this->_currHandle = CreateEvent(NULL, TRUE, FALSE, NULL));
		if (!this->_currHandle)
			return -1;

		//Сообщаем драйверу идентификатор события прерывания
		tmkdefevent(this->_currHandle, true);
		tmkgetevd(events);

		if (bcreset() != 0)
			return -1;

		//Захват базы
		if (bcdefbase(this->_wBase) != 0) {
			CloseHandle(this->_currHandle);
			tmkdone(this->_setPlate);
			return -1;
		}
		this->_captured = true;
	}
	else {
		if (this->_captured) {
			CloseHandle(this->_currHandle);
			tmkdone(this->_setPlate);
			this->_captured = false;
		}
		return -1;
	}
	return this->_setPlate;
}


// Метод, производящий освобождение устройства.
bool Manchester::toFreePlate(int currentPlate) {

	//Сброс выбранной базы, события и устройства
	if (bcreset() != 0) {
		tmkdone(currentPlate);
		return false;
	}
	CloseHandle(this->_currHandle);
	tmkdone(currentPlate);
	this->_captured = false;
	return true;
}

// Метод, выполняющий действие - "отправили" (единичный запрос)
bool Manchester::sendData(short oldAddr, short addrOy, short K, short subAddr, short ncd, unsigned short * massToSend) {
	unsigned short *awBuf = new unsigned short[ncd];
	unsigned short awBufOut[32] = { 0 };
	this->_addrOY = addrOy;
	this->_currSubAddr = subAddr;

	//Обнуление выходного буфера с АЭ
	for (int i = 0; i < ncd; i++)
		awBuf[i] = 0;
	if (K == 0) {
		//****************    //Для установки адреса комментарий снять
		if (subAddr == 1)
			addrOy = oldAddr;
		//this->_currSubAddr = 17;  // ?  понятия не имею зачем делал так
		//*****************
		this->_format = 1;
		awBuf = massToSend;//generateDW(this->_mode, ncd, Frame);
		if (awBuf == nullptr)
			return false;
		//*******************
		for (int i = 0; i < ncd; i++) {
			awBufOut[i] = awBuf[i];
			this->_sendingMass[i] = awBuf[i];
		}
		bcputblk(1, awBufOut, ncd);
		//bcputblk(1, awBufOut, ncd);
		// Отправка данных
		bcputw(this->_wBase, this->_command = CW(addrOy, RT_RECEIVE, subAddr, ncd));
		do {
			if (bcstartx(this->_wBase, sendingControl(K) | CX_STOP | _milstdBus | CX_NOSIG) != 0)
				return false;
			if (WaitInt(sendingControl(K)))
				return false;
			this->_countOfErrors++;
			if (this->_countOfErrors == 10) {
				//Проверить какое ОС приходит
				errorParse(bcgetw(1));
				//errorParse(event.bcx.wResultX);
				this->_countOfErrors = 0;
				return false;
			}
		} while ((events->bcx.wResultX & (SX_ERR_MASK | SX_IB_MASK)) != 0);
		this->_countOfErrors = 0; //отправка успешна, вырубаем
	}//k==0
	else {
		this->_format = 2;
		// ************************// Получение данных ************************
		bcputw(this->_wBase, this->_command = CW(addrOy, RT_TRANSMIT, subAddr, ncd));
		do {
			if (bcstartx(this->_wBase, sendingControl(K) | CX_STOP | _milstdBus | CX_NOSIG) != 0)
				return false;
			if (WaitInt(sendingControl(K)))
				return false;
			this->_countOfErrors++;
			if (this->_countOfErrors == 10) {
				errorParse(bcgetw(1));
				//errorParse(event.bcx.wResultX);
				this->_countOfErrors = 0;
				return false;
			}
		}//do
		while ((events->bcx.wResultX & (SX_ERR_MASK | SX_IB_MASK)) != 0);
		this->_countOfErrors = 0; //отправка успешна, вырубаем
		//Получаем данные
		bcgetblk(this->_format, awBufOut, ncd);
		//**********************************************************
		// Проверка данных
		//**********************************************************
		//if ((subAddr == 4) || (subAddr == 8) || (subAddr == 12)) {
		//	for (int i = 1; i < ncd; i++) {
		//		if (this->_sendingMass[i] != awBufOut[i])
		//			return false;
		//	}
		//}
		//**********************************************************
		for (int i = 0; i < ncd; i++)
			massToSend[i] = awBufOut[i];

	} //else
	for (int i = 0; i < ncd; i++)
		this->_receivedData[i] = awBufOut[i];
	//delete[] awBufOut;
	//delete[]awBuf;
	return true;
}

void Manchester::errorParse(short OC) {
	if ((OC & ERROR_MASK) == ERROR_MASK)
		this->_errorCode = -4; //Ошибка в сообщении
	else if ((OC & BUSY_MASK) == BUSY_MASK)
		this->_errorCode = -8; //Устройство занято
	else if ((OC & RTFL_MASK) == RTFL_MASK)
		this->_errorCode = -1; //Неисправность устройства
	else if ((OC & S_TO_MASK) == S_TO_MASK)
		this->_errorCode = -10; //Плата не захвачена
}