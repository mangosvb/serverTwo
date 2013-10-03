' 
' Copyright (C) 2013 getMaNGOS <http://www.getmangos.co.uk>
'
' This program is free software; you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation; either version 2 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
'
' You should have received a copy of the GNU General Public License
' along with this program; if not, write to the Free Software
' Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
'
Imports System.Runtime.InteropServices
Imports System.IO
Imports System.CodeDom
Imports System.Reflection
Imports System.CodeDom.Compiler
Imports System.Runtime.CompilerServices


Namespace PacketLogger
    Public Class ConsoleColorClass
        Public Enum ForegroundColors
            Black = 0
            Blue = 1
            Green = 2
            Cyan = Blue Or Green
            Red = 4
            Magenta = Blue Or Red
            Yellow = Green Or Red
            White = Blue Or Green Or Red
            Gray = 8
            LightBlue = Gray Or Blue
            LightGreen = Gray Or Green
            LightCyan = Gray Or Cyan
            LightRed = Gray Or Red
            LightMagenta = Gray Or Magenta
            LightYellow = Gray Or Yellow
            BrightWhite = Gray Or White
        End Enum
        Public Enum BackgroundColors
            Black = 0
            Blue = 16
            Green = 32
            Cyan = Blue Or Green
            Red = 64
            Magenta = Blue Or Red
            Yellow = Green Or Red
            White = Blue Or Green Or Red
            Gray = 128
            LightBlue = Gray Or Blue
            LightGreen = Gray Or Green
            LightCyan = Gray Or Cyan
            LightRed = Gray Or Red
            LightMagenta = Gray Or Magenta
            LightYellow = Gray Or Yellow
            BrightWhite = Gray Or White
        End Enum
        Public Enum Attributes
            None = &H0
            GridHorizontal = &H400
            GridLVertical = &H800
            GridRVertical = &H1000
            ReverseVideo = &H4000
            Underscore = &H8000
        End Enum

        Private Const STD_OUTPUT_HANDLE As Integer = -11
        Private Shared InvalidHandleValue As New IntPtr(-1)

        Public Sub New()
            ' This class can not be instantiated.
        End Sub

        ' Our wrapper implementations.
        Public Sub SetConsoleColor()
            SetConsoleColor(ForegroundColors.White, BackgroundColors.Black)
        End Sub
        Public Sub SetConsoleColor(ByVal foreground As ForegroundColors)
            SetConsoleColor(foreground, BackgroundColors.Black, Attributes.None)
        End Sub
        Public Sub SetConsoleColor(ByVal foreground As ForegroundColors, ByVal background As BackgroundColors)
            SetConsoleColor(foreground, background, Attributes.None)
        End Sub
        Public Sub SetConsoleColor(ByVal foreground As ForegroundColors, ByVal background As BackgroundColors, ByVal attribute As Attributes)
            Dim handle As IntPtr = GetStdHandle(STD_OUTPUT_HANDLE)
            If handle.Equals(InvalidHandleValue) Then
                Throw New System.ComponentModel.Win32Exception
            End If
            ' We have to convert the integer flag values into a Unsigned Short (UInt16) to pass to the 
            ' SetConsoleTextAttribute API call.
            Dim value As UInt16 = System.Convert.ToUInt16(foreground Or background Or attribute)
            If Not SetConsoleTextAttribute(handle, value) Then
                Throw New System.ComponentModel.Win32Exception
            End If
        End Sub

        ' DLLImport's (Win32 functions)
        <DllImport("Kernel32.dll", SetLastError:=True)> Private Shared Function GetStdHandle(ByVal stdHandle As Integer) As IntPtr
        End Function
        <DllImport("Kernel32.dll", SetLastError:=True)> Private Shared Function SetConsoleTextAttribute(ByVal consoleOutput As IntPtr, ByVal Attributes As UInt16) As Boolean
        End Function
        <DllImport("Kernel32.dll", SetLastError:=True)> Public Shared Function SetConsoleTitle(ByVal ConsoleTitle As String) As Boolean
        End Function
    End Class
    Public Class PacketClass
        Implements IDisposable

        Public Data() As Byte
        Public Offset As Integer = 4

        Public ReadOnly Property Length() As Integer
            Get
                Return (Data(1) + (Data(0) * 256))
            End Get
        End Property
        Public ReadOnly Property OpCode() As Integer
            Get
                Return (Data(2) + (Data(3) * 256))
            End Get
        End Property

        Public Sub New(ByVal opcode As Int16)
            ReDim Preserve Data(3)
            Data(0) = 0
            Data(1) = 2
            Data(2) = opcode Mod 256
            Data(3) = opcode \ 256
        End Sub
        Public Sub New(ByRef rawdata() As Byte)
            Data = rawdata
            rawdata.CopyTo(Data, 0)
        End Sub


        Public Sub AddBitArray(ByVal buffer As BitArray, ByVal Len As Integer)
            ReDim Preserve Data(Data.Length - 1 + Len)
            Data(0) = (Data.Length - 2) \ 256
            Data(1) = (Data.Length - 2) Mod 256

            Dim bufferarray(CType((buffer.Length + 8) / 8, Byte)) As Byte

            buffer.CopyTo(bufferarray, 0)
            Array.Copy(bufferarray, 0, Data, Data.Length - Len, Len)
        End Sub
        Public Sub AddInt8(ByVal buffer As Byte)
            ReDim Preserve Data(Data.Length)
            Data(0) = (Data.Length - 2) \ 256
            Data(1) = (Data.Length - 2) Mod 256
            Data(Data.Length - 1) = buffer
        End Sub
        Public Sub AddInt16(ByVal buffer As Integer)
            ReDim Preserve Data(Data.Length + 1)
            Data(0) = (Data.Length - 2) \ 256
            Data(1) = (Data.Length - 2) Mod 256

            'Data(Data.Length - 2) = buffer Mod 256
            'Data(Data.Length - 1) = buffer \ 256

            Data(Data.Length - 2) = CType((buffer And 255), Byte)
            Data(Data.Length - 1) = CType(((buffer >> 8) And 255), Byte)
        End Sub
        Public Sub AddInt32(ByVal buffer As Integer)
            ReDim Preserve Data(Data.Length + 3)
            Data(0) = (Data.Length - 2) \ 256
            Data(1) = (Data.Length - 2) Mod 256

            'Data(Data.Length - 4) = buffer Mod 256
            'Data(Data.Length - 3) = (buffer \ 256) Mod 256
            'Data(Data.Length - 2) = ((buffer \ 256) \ 256) Mod 256
            'Data(Data.Length - 1) = ((buffer \ 256) \ 256) \ 256

            Data(Data.Length - 4) = CType((buffer And 255), Byte)
            Data(Data.Length - 3) = CType(((buffer >> 8) And 255), Byte)
            Data(Data.Length - 2) = CType(((buffer >> 16) And 255), Byte)
            Data(Data.Length - 1) = CType(((buffer >> 24) And 255), Byte)
        End Sub
        Public Sub AddInt64(ByVal buffer As Long)
            ReDim Preserve Data(Data.Length + 7)
            Data(0) = (Data.Length - 2) \ 256
            Data(1) = (Data.Length - 2) Mod 256

            Data(Data.Length - 8) = CType((buffer And 255), Byte)
            Data(Data.Length - 7) = CType(((buffer >> 8) And 255), Byte)
            Data(Data.Length - 6) = CType(((buffer >> 16) And 255), Byte)
            Data(Data.Length - 5) = CType(((buffer >> 24) And 255), Byte)
            Data(Data.Length - 4) = CType(((buffer >> 32) And 255), Byte)
            Data(Data.Length - 3) = CType(((buffer >> 40) And 255), Byte)
            Data(Data.Length - 2) = CType(((buffer >> 48) And 255), Byte)
            Data(Data.Length - 1) = CType(((buffer >> 56) And 255), Byte)
        End Sub
        'Not Tested!!!
        Public Sub AddString(ByVal buffer As String)
            ReDim Preserve Data(Data.Length + Len(buffer) - 1)
            Data(0) = (Data.Length - 2) \ 256
            Data(1) = (Data.Length - 2) Mod 256

            Dim chArray1 As Char() = buffer.ToCharArray
            Dim chArray2 As Char() = chArray1
            Dim num1 As Integer
            For num1 = 0 To chArray2.Length - 1
                Data(Data.Length - Len(buffer) + num1) = CType(Asc(chArray2(num1)), Byte)
            Next num1
            Me.AddInt8(0)
        End Sub
        Public Sub AddDouble(ByVal buffer2 As Double)
            Dim buffer1 As Byte() = BitConverter.GetBytes(buffer2)
            ReDim Preserve Data(Data.Length + buffer1.Length - 1)
            Buffer.BlockCopy(buffer1, 0, Data, Data.Length - buffer1.Length, buffer1.Length)

            Data(0) = (Data.Length - 2) \ 256
            Data(1) = (Data.Length - 2) Mod 256
        End Sub
        Public Sub AddSingle(ByVal buffer2 As Single)
            Dim buffer1 As Byte() = BitConverter.GetBytes(buffer2)
            ReDim Preserve Data(Data.Length + buffer1.Length - 1)
            Buffer.BlockCopy(buffer1, 0, Data, Data.Length - buffer1.Length, buffer1.Length)

            Data(0) = (Data.Length - 2) \ 256
            Data(1) = (Data.Length - 2) Mod 256
        End Sub
        Public Sub AddByteArray(ByVal buffer() As Byte)
            Dim tmp As Integer = Data.Length
            ReDim Preserve Data(Data.Length + buffer.Length - 1)
            Array.Copy(buffer, 0, Data, tmp, buffer.Length)

            Data(0) = (Data.Length - 2) \ 256
            Data(1) = (Data.Length - 2) Mod 256
        End Sub



        Public Function GetInt8() As Byte
            Offset = Offset + 1
            Return Data(Offset - 1)
        End Function
        Public Function GetInt8(ByRef Offset As Integer) As Byte
            Offset = Offset + 1
            Return Data(Offset - 1)
        End Function
        Public Function GetInt16() As Short
            Dim num1 As Short = BitConverter.ToInt16(Data, Offset)
            Offset = (Offset + 2)
            Return num1
        End Function
        Public Function GetInt16(ByRef Offset As Integer) As Short
            Dim num1 As Short = BitConverter.ToInt16(Data, Offset)
            Offset = (Offset + 2)
            Return num1
        End Function
        Public Function GetInt32() As Integer
            Dim num1 As Integer = BitConverter.ToInt32(Data, Offset)
            Offset = (Offset + 4)
            Return num1
        End Function
        Public Function GetInt32(ByRef Offset As Integer) As Integer
            Dim num1 As Integer = BitConverter.ToInt32(Data, Offset)
            Offset = (Offset + 4)
            Return num1
        End Function
        Public Function GetInt64() As Long
            Dim num1 As Long = BitConverter.ToInt64(Data, Offset)
            Offset = (Offset + 8)
            Return num1
        End Function
        Public Function GetInt64(ByRef Offset As Integer) As Long
            Dim num1 As Long = BitConverter.ToInt64(Data, Offset)
            Offset = (Offset + 8)
            Return num1
        End Function
        Public Function GetFloat() As Single
            Dim single1 As Single = BitConverter.ToSingle(Data, Offset)
            Offset = (Offset + 4)
            Return single1
        End Function
        Public Function GetFloat(ByRef Offset As Integer) As Single
            Dim single1 As Single = BitConverter.ToSingle(Data, Offset)
            Offset = (Offset + 4)
            Return single1
        End Function
        Public Function GetDouble() As Double
            Dim num1 As Double = BitConverter.ToDouble(Data, Offset)
            Offset = (Offset + 8)
            Return num1
        End Function
        Public Function GetDouble(ByRef Offset As Integer) As Double
            Dim num1 As Double = BitConverter.ToDouble(Data, Offset)
            Offset = (Offset + 8)
            Return num1
        End Function
        Public Function GetString() As String
            Dim i As Integer = Offset
            Dim tmpString As String = ""
            While Data(i) <> 0
                tmpString = tmpString + Chr(Data(i))
                i = i + 1
                Offset = Offset + 1
            End While
            Offset = Offset + 1
            Return tmpString
        End Function
        Public Function GetString(ByRef Offset As Integer) As String
            Dim i As Integer = Offset
            Dim tmpString As String = ""
            While Data(i) <> 0
                tmpString = tmpString + Chr(Data(i))
                i = i + 1
                Offset = Offset + 1
            End While
            Offset = Offset + 1
            Return tmpString
        End Function


        Public Sub Dispose() Implements System.IDisposable.Dispose
        End Sub
    End Class
    Public Enum packetType
        RECV_PACKET = 0
        SENT_PACKET = 1
    End Enum
    Public Enum packetSource
        RS = 0
        WS = 1
    End Enum

    'Dim test As New ScriptedObject("scripts\test.vb", "test.dll")
    'test.Invoke(".TestScript", "TestMe")

    'creature = test.Invoke("DefaultAI_1")
    'creature.Move()
    Public Class ScriptedObject
        Implements IDisposable

        Private compRes As CompilerResults
        Private ass As [Assembly]

        Public Sub New(ByVal AssemblySourceFile As String, ByVal AssemblyFile As String, ByVal InMemory As Boolean)
            Logger.ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.Green)
            Console.WriteLine("[{0}] Compiling: {1}", Format(TimeOfDay, "hh:mm:ss"), AssemblySourceFile)
            Logger.ConsoleColor.SetConsoleColor()

            Try
                Dim VBcp As New VBCodeProvider
                Dim cg As ICodeGenerator = VBcp.CreateGenerator()
                'Dim AssemblySourceFile As String = "MyClass.vb"
                'Dim AssemblyFile As String = "MyClass.dll"

                ' Generating the assembly source file
                'Dim tw As TextWriter
                'tw = New StreamWriter(New FileStream(AssemblySourceFile, FileMode.Create))
                'GenerateEvalMethod(tw, Expression, cg)
                'tw.Close()

                ' Compiling the source file to an assembly DLL
                Dim cc As ICodeCompiler = VBcp.CreateCompiler()
                Dim cpar As New CompilerParameters
                If Not InMemory Then cpar.OutputAssembly = AssemblyFile
                cpar.ReferencedAssemblies.Add("System.dll")
                cpar.ReferencedAssemblies.Add("System.Data.dll")
                cpar.ReferencedAssemblies.Add("Microsoft.VisualBasic.dll")
                cpar.ReferencedAssemblies.Add(AppDomain.CurrentDomain.FriendlyName)
                'cpar.ReferencedAssemblies.Add("PacketLogger.exe")
                cpar.GenerateExecutable = False     ' result is a .DLL
                cpar.GenerateInMemory = InMemory
                'Dim compRes As CompilerResults
                compRes = cc.CompileAssemblyFromFile(cpar, System.AppDomain.CurrentDomain.BaseDirectory() & AssemblySourceFile)

                If compRes.Errors.HasErrors = True Then
                    For Each err As System.Codedom.Compiler.CompilerError In compRes.Errors
                        Logger.ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.LightRed)
                        Console.WriteLine("[{0}] Compiling: Error on line {2}:{1}{3}", Format(TimeOfDay, "hh:mm:ss"), vbNewLine, err.Line, err.ErrorText)

                        Logger.ConsoleColor.SetConsoleColor()
                    Next
                End If

                ass = compRes.CompiledAssembly
                'Dim ass As [Assembly] = [Assembly].LoadFrom("test.dll")
            Catch e As Exception
                Logger.ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.LightRed)
                'Console.WriteLine("[{0}] Unable to compile script [{1}]. {3}{2}", Format(TimeOfDay, "hh:mm:ss"), AssemblySourceFile, e.ToString, vbNewLine)
                Console.WriteLine("[{0}] Unable to compile script [{1}].", Format(TimeOfDay, "hh:mm:ss"), AssemblySourceFile, e.ToString, vbNewLine)
                Logger.ConsoleColor.SetConsoleColor()
            End Try
        End Sub
        Public Sub Invoke(ByVal MyModule As String, ByVal MyMethod As String, Optional ByVal Parameters As Object = Nothing)
            Try
                Dim ty As Type = ass.GetType("PacketLogger." & MyModule)
                Dim mi As MethodInfo = ty.GetMethod(MyMethod)
                mi.Invoke(Nothing, Parameters)
            Catch e As TargetInvocationException
                Logger.ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.LightRed)
                Console.WriteLine("WARNING: Script execution error:{2}{1}", Format(TimeOfDay, "hh:mm:ss"), e.GetBaseException.ToString, vbNewLine)
                'Console.WriteLine("WARNING: Script execution error:{2}{1}", Format(TimeOfDay, "hh:mm:ss"), e.InnerException.ToString, vbNewLine)
                Logger.ConsoleColor.SetConsoleColor()
            Catch e As Exception
                Logger.ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.LightRed)
                Console.WriteLine("WARNING: Script Method [{0}] not found in [PacketLogger.{1}]!", MyMethod, MyModule)
                Logger.ConsoleColor.SetConsoleColor()
            End Try
            'Dim bflags As BindingFlags = BindingFlags.DeclaredOnly Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance
            'Dim obj As Object = ty.InvokeMember(MyMethod, bflags Or BindingFlags.CreateInstance, Nothing, Nothing, Nothing)

            'ty.InvokeMember(MyMethod, bflags Or BindingFlags.InvokeMethod, Nothing, obj, Nothing)
        End Sub
        Public Function Invoke(ByVal MyBaseClass As String, Optional ByVal Parameters As Object = Nothing) As Object
            Try
                Dim ty As Type = ass.GetType("PacketLogger." & MyBaseClass)
                Dim ci() As ConstructorInfo = ty.GetConstructors

                Return ci(0).Invoke(Parameters)
            Catch e As TargetInvocationException
                Logger.ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.LightRed)
                Console.WriteLine("WARNING: Script execution error:{2}{1}", Format(TimeOfDay, "hh:mm:ss"), e.GetBaseException.ToString, vbNewLine)
                'Console.WriteLine("WARNING: Script execution error:{2}{1}", Format(TimeOfDay, "hh:mm:ss"), e.InnerException.ToString, vbNewLine)
                Logger.ConsoleColor.SetConsoleColor()
            Catch e As Exception
                Logger.ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.LightRed)
                Console.WriteLine("WARNING: Scripted Class [{0}] not found in [PacketLogger]!", MyBaseClass)
                Logger.ConsoleColor.SetConsoleColor()
                Console.WriteLine(e.ToString)
            End Try
            'Dim bflags As BindingFlags = BindingFlags.DeclaredOnly Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance
            'Dim obj As Object = ty.InvokeMember(MyMethod, bflags Or BindingFlags.CreateInstance, Nothing, Nothing, Nothing)

            'ty.InvokeMember(MyMethod, bflags Or BindingFlags.InvokeMethod, Nothing, obj, Nothing)
        End Function
        Public Function ContainsMethod(ByVal MyModule As String, ByVal MyMethod As String) As Boolean
            Dim ty As Type = ass.GetType("PacketLogger." & MyModule)
            Dim mi As MethodInfo = ty.GetMethod(MyMethod)
            If mi Is Nothing Then Return False Else Return True
        End Function
        Public Sub Dispose() Implements System.IDisposable.Dispose
        End Sub
    End Class


    Public Class BaseFileLogger
        Implements IDisposable

        Private file As TextWriter
        Public Sub New(Optional ByVal BaseFileName as String = "PacketDump")
            file = New StreamWriter(New FileStream(String.Format("{1}-{0}.log", Format(Now, "yyyy-MMM-d-H-mm"),BaseFileName), FileMode.Create))
        End Sub
        Public Sub Log(Optional ByVal text As String = "")
            file.WriteLine(text)
            file.Flush()
        End Sub
        Public Sub LogPacket(ByRef packet As PacketClass, ByVal packetType As packetType)
            LogPacket(packet.Data, packetType)
        End Sub

        <MethodImplAttribute(MethodImplOptions.Synchronized)> _
        Public Sub LogPacket(ByRef data() As Byte, ByVal packetType As packetType, Optional ByVal Source As packetSource = packetSource.WS)
            Dim tmp As String
            If Source = packetSource.WS Then
                If packetType = packetType.RECV_PACKET Then
                    tmp = String.Format("R/O: {0}/{1}[{2}]", data.Length, CType(data(2) + (data(3) * 256), OPCODES), data(2) + (data(3) * 256))
                Else
                    tmp = String.Format("S/O: {0}/{1}[{2}]" ,data.Length , CType(data(2) + (data(3) * 256), OPCODES) , data(2) + (data(3) * 256))
                End If
                Console.WriteLine("[{1}] {0}", CType(data(2) + (data(3) * 256), OPCODES), CType(data(2) + (data(3) * 256), Integer))
            Else
                If packetType = packetType.RECV_PACKET Then
                    tmp = String.Format("R/O: " & data.Length & "/" & RSOpcodeNames(CType(data(0), Integer)) & "[" & data(0) & "]")
                Else
                    tmp = String.Format("S/O: " & data.Length & "/" & RSOpcodeNames(CType(data(0), Integer)) & "[" & data(0) & "]")
                End If
                Console.WriteLine("[{1}] {0}",RSOpcodeNames(CType(data(0), Integer)), data(0))
            End If

            tmp = tmp & vbNewLine & Logger.DumpPacket(data)
            file.WriteLine(tmp)
            file.Flush()
            'Console.WriteLine(tmp)
        End Sub
        Public Sub Dispose() Implements System.IDisposable.Dispose
            file.Close()
        End Sub
    End Class
    Public Class BasePacketHandler
        Public Overridable Sub OnRecv(ByRef packet As PacketClass)

        End Sub
    End Class



End Namespace


