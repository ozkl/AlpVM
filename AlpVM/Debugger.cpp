#include <stdio.h>

#include "VirtualMachine.h"

#include "Debugger.h"

static bool sInitialized = false;

Debugger::Debugger(VirtualMachine* machine)
	:
	mMachine(machine),
	mListenerSocket(NULL),
	mConnectionSocket(NULL),
	mSocketSet(NULL)
{
	if (!sInitialized)
	{
		SDLNet_Init();

		sInitialized = true;
	}

	mSocketSet = SDLNet_AllocSocketSet(1);
}


Debugger::~Debugger()
{
	close();

	SDLNet_FreeSocketSet(mSocketSet);

	if (sInitialized)
	{
		SDLNet_Quit();

		sInitialized = false;
	}
}

bool Debugger::listen(short port)
{
	IPaddress ip;
	if (SDLNet_ResolveHost(&ip, NULL, port) == -1)
	{
		return false;
	}

	mListenerSocket = SDLNet_TCP_Open(&ip);

	if (!mListenerSocket)
	{
		return false;
	}

	return true;
}

void Debugger::close()
{
	closeConnection();

	if (NULL != mListenerSocket)
	{
		SDLNet_TCP_Close(mListenerSocket);
		mListenerSocket = NULL;
	}
}

bool Debugger::tryAccept()
{
	if (NULL == mListenerSocket)
	{
		return false;
	}

	mConnectionSocket = SDLNet_TCP_Accept(mListenerSocket);

	if (NULL != mConnectionSocket)
	{
		SDLNet_TCP_AddSocket(mSocketSet, mConnectionSocket);

		sendMachineState();

		return true;
	}
	
	return false;
}

void Debugger::closeConnection()
{
	if (NULL != mConnectionSocket)
	{
		SDLNet_TCP_DelSocket(mSocketSet, mConnectionSocket);
		SDLNet_TCP_Close(mConnectionSocket);
		mConnectionSocket = NULL;
	}
}

void Debugger::process()
{
	if (!isConnected())
	{
		tryAccept();
	}

	//int s = sizeof(MachineState);
	const int commandSize = sizeof(DebugCommand);

	DebugCommand command;

	if (isConnected())
	{
		int check = SDLNet_CheckSockets(mSocketSet, 0);

		if (check > 0)
		{
			int ready = SDLNet_SocketReady(mConnectionSocket);
			if (ready > 0)
			{
				int length = SDLNet_TCP_Recv(mConnectionSocket, mBuffer, BufferSize);
				if (commandSize == length)
				{
					command = (DebugCommand)*(DebugCommand*)mBuffer;
				}
				else if (length <= 0)
				{
					//Closed connection, so cleaning up
					closeConnection();
				}
			}
		}
	}

	switch (command.Command)
	{
	case DebugCommand::EnCommand::Pause:
		mMachine->pause();
		sendMachineState();
		break;
	case DebugCommand::EnCommand::Resume:
		mMachine->resume();
		sendMachineState();
		break;
	case DebugCommand::EnCommand::Step:
	{
		mMachine->executeInstruction();
		
		sendMachineState();
	}
		break;
	case DebugCommand::EnCommand::GetMemory:
		sendMemory(command.Data);
		break;
	default:
		if (!mMachine->isPaused())
		{
			mMachine->executeInstruction();
		}
		break;
	}
	
}

void Debugger::sendMachineState()
{
	if (isConnected() == false)
	{
		return;
	}

	char* buffer = new char[sizeof(VmMessageHeader) + sizeof(MachineState)];
	VmMessageHeader* header = (VmMessageHeader*)buffer;
	header->MessageType = VmMessageHeader::EnMessageType::MachineState;
	header->MessageSize = sizeof(MachineState);
	header->Data = 0;
	memcpy(buffer + sizeof(VmMessageHeader), mMachine->getMachineState(), sizeof(MachineState));

	SDLNet_TCP_Send(mConnectionSocket, buffer, sizeof(VmMessageHeader) + sizeof(MachineState));

	delete[] buffer;
}

void Debugger::sendMemory(unsigned int offset)
{
	if (isConnected() == false)
	{
		return;
	}

	//TODO: Modify offset so that instructions fit entirely
	//TODO: check size for not accessing end of memory

	unsigned int size = 960;//16*60

	char* buffer = new char[sizeof(VmMessageHeader) + size];
	
	VmMessageHeader* header = (VmMessageHeader*)buffer;
	header->MessageType = VmMessageHeader::EnMessageType::Memory;
	header->MessageSize = size;
	header->Data = offset;
	
	memcpy(buffer + sizeof(VmMessageHeader), mMachine->getMemory() + offset, size);

	SDLNet_TCP_Send(mConnectionSocket, buffer, sizeof(VmMessageHeader) + size);

	delete[] buffer;
}