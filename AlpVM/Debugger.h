#ifndef DEBUGGER_H
#define DEBUGGER_H

#include <SDL_net.h>

class VirtualMachine;

const int BufferSize = 1024;

class Debugger
{
public:
	struct DebugCommand
	{
		enum EnCommand
		{
			NoCommand,
			Pause,
			Resume,
			Step,
			SetBreakpoint,
			UnsetBreakpoint,
			GetBreakpoints,
			ClearBreakpoints,
			GetMemory
		};

		DebugCommand()
		{
			Command = EnCommand::NoCommand;
			Data = 0;
		}

		EnCommand Command;
		unsigned int Data;
	};

	struct VmMessageHeader
	{
		enum EnMessageType
		{
			MachineState,
			Memory,
			Breakpoints
		};

		VmMessageHeader()
		{
			MessageType = MachineState;
			MessageSize = 0;
			Data = 0;
		}

		EnMessageType MessageType;

		unsigned int MessageSize;
		unsigned int Data;
	};

	Debugger(VirtualMachine* machine);
	~Debugger();

	bool listen(short port);
	void close();
	inline bool isConnected()
	{
		return mConnectionSocket != NULL;
	}

	void process();

	void sendMachineState();
	void sendMemory(unsigned int offset);

private:
	bool tryAccept();
	void closeConnection();

private:
	VirtualMachine* mMachine;
	char mBuffer[BufferSize];
	TCPsocket mListenerSocket;
	TCPsocket mConnectionSocket;
	SDLNet_SocketSet mSocketSet;

};
#endif //DEBUGGER_H
