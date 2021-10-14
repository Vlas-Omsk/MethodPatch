# MethodPatch
Allows you to patch a method at runtime and intercept its call 

# Tests .NET Framework 4.7.2
X32
  Debug x32 (with debugger)   | + |
  Debug x32                   | + |
  Release x32 (with debugger) | + |
  Release x32                 | + |
X64
  Debug x64 (with debugger)   | - | Little space
  Debug x64                   | ~ | AccessViolationException at the end
  Release x64 (with debugger) | - | AccessViolationException
  Release x64                 | + |
