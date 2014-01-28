#ifndef OPCODE_H
#define OPCODE_H
enum opcode
{
	INVALID,
	DLOAD,
	ILOAD,
	SLOAD,
	DLOAD0,
	ILOAD0,
	SLOAD0,
	DLOAD1,
	ILOAD1,
	DLOADM1,
	ILOADM1,
	DADD,
	IADD,
	DSUB,
	ISUB,
	DMUL,
	IMUL,
	DDIV,
	IDIV,
	IMOD,
	DNEG,
	INEG,
	IAOR,
	IAAND,
	IAXOR,
	IPRINT,
	DPRINT,
	SPRINT,
	I2D,
	D2I,
	S2I,
	SWAP,
	POP,
	LOADDVAR0,
	LOADDVAR1,
	LOADDVAR2,
	LOADDVAR3,
	LOADIVAR0,
	LOADIVAR1,
	LOADIVAR2,
	LOADIVAR3,
	LOADSVAR0,
	LOADSVAR1,
	LOADSVAR2,
	LOADSVAR3,
	STOREDVAR0,
	STOREDVAR1,
	STOREDVAR2,
	STOREDVAR3,
	STOREIVAR0,
	STOREIVAR1,
	STOREIVAR2,
	STOREIVAR3,
	STORESVAR0,
	STORESVAR1,
	STORESVAR2,
	STORESVAR3,
	LOADDVAR,
	LOADIVAR,
	LOADSVAR,
	STOREDVAR,
	STOREIVAR,
	STORESVAR,
	LOADCTXDVAR,
	LOADCTXIVAR,
	LOADCTXSVAR,
	STORECTXDVAR,
	STORECTXIVAR,
	STORECTXSVAR,
	DCMP,
	ICMP,
	JA,
	IFICMPNE,
	IFICMPE,
	IFICMPG,
	IFICMPGE,
	IFICMPL,
	IFICMPLE,
	DUMP,
	STOP,
	CALL,
	RETURN,
	BREAK
};
#endif