#include <tblgen/Record.h>
#include <tblgen/Value.h>

#include <llvm/ADT/ArrayRef.h>
#include <llvm/Support/Casting.h>
#include <llvm/Support/raw_ostream.h>

#include <iostream>
#include <string>
#include <vector>

using namespace tblgen;

namespace
{

class EmitCommandsBackend {
    std::ostream &OS;
    RecordKeeper &RK;
    std::vector<Record*> commands;

public:
    EmitCommandsBackend(std::ostream &OS, RecordKeeper &RK) : OS(OS), RK(RK)
    {

    }

    void Emit();
};

} // anonymous namespace


void EmitCommandsBackend::Emit()
{
    RK.getAllDefinitionsOf("Command", commands);

    llvm::raw_string_ostream parseCommand

    OS << R"__(
using System.Collections.Generic;

namespace Transidious
{

public class DeveloperConsoleInternals
{
    public void ParseCommand(string rawCmd)
    {
        
    }
}

}

)__";
}

extern "C"
{
    void EmitCommands(std::ostream &OS, RecordKeeper &RK)
    {
        EmitCommandsBackend(OS, RK).Emit();
    }
};