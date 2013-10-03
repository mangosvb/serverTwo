'
' Copyright (C) 2013 getMaNGOS <http://www.getMangos.co.uk>
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
Imports System.Text
Imports System.Security.Cryptography
Imports System.IO

Public Class AuthEngineClass
    Implements IDisposable

#Region "AuthEngine.Constructive"
    Shared Sub New()
        AuthEngineClass.CrcSalt = New Byte(16 - 1) {}
        AuthEngineClass.RAND_bytes(CrcSalt, 16)

    End Sub
    Public Sub New()
        Dim buffer1 As Byte() = New Byte() {7}
        Me.g = buffer1
        Me.N = New Byte() {137, 75, 100, 94, 137, 225, 83, 91, 189, 173, 91, 139, 41, 6, 80, 83, 8, 1, 177, 142, 191, 191, 94, 143, 171, 60, 130, 135, 42, 62, 155, 183}
        Me.salt = New Byte() {173, 208, 58, 49, 210, 113, 20, 70, 117, 242, 112, 126, 80, 38, 182, 210, 241, 134, 89, 153, 118, 2, 80, 170, 185, 69, 224, 158, 221, 42, 163, 69}
        Dim buffer2 As Byte() = New Byte() {3}
        Me.k = buffer2
        Me.PublicB = New Byte(32 - 1) {}
        Me.b = New Byte(20 - 1) {}
    End Sub

    Public Declare Function BN_add Lib "LIBEAY32" (ByVal r As IntPtr, ByVal a As IntPtr, ByVal b As IntPtr) As Integer
    Public Declare Function BN_bin2bn Lib "LIBEAY32" (ByVal ByteArrayIn As Byte(), ByVal length As Integer, ByVal [to] As IntPtr) As IntPtr
    Public Declare Function BN_bn2bin Lib "LIBEAY32" (ByVal a As IntPtr, ByVal [to] As Byte()) As Integer
    Public Declare Function BN_CTX_free Lib "LIBEAY32" (ByVal a As IntPtr) As Integer
    Public Declare Function BN_CTX_new Lib "LIBEAY32" () As IntPtr
    Public Declare Function BN_mod Lib "LIBEAY32" (ByVal r As IntPtr, ByVal a As IntPtr, ByVal b As IntPtr, ByVal ctx As IntPtr) As Integer
    Public Declare Function BN_mod_exp Lib "LIBEAY32" (ByVal res As IntPtr, ByVal a As IntPtr, ByVal p As IntPtr, ByVal m As IntPtr, ByVal ctx As IntPtr) As IntPtr
    Public Declare Function BN_mul Lib "LIBEAY32" (ByVal r As IntPtr, ByVal a As IntPtr, ByVal b As IntPtr, ByVal ctx As IntPtr) As Integer
    Public Declare Function BN_new Lib "LIBEAY32" () As IntPtr

    Public Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
#End Region

#Region "AuthEngine.Calculations"
    Private Sub CalculateB()
        Dim encoding1 As New UTF7Encoding
        AuthEngineClass.RAND_bytes(Me.b, 20)
        Dim ptr1 As IntPtr = AuthEngineClass.BN_New
        Dim ptr2 As IntPtr = AuthEngineClass.BN_New
        Dim ptr3 As IntPtr = AuthEngineClass.BN_New
        Dim ptr4 As IntPtr = AuthEngineClass.BN_New
        Me.BNPublicB = AuthEngineClass.BN_New
        Dim ptr5 As IntPtr = AuthEngineClass.BN_ctx_new
        Array.Reverse(Me.b)
        Me.BNb = AuthEngineClass.BN_Bin2BN(Me.b, Me.b.Length, IntPtr.Zero)
        Array.Reverse(Me.b)
        AuthEngineClass.BN_mod_exp(ptr1, Me.BNg, Me.BNb, Me.BNn, ptr5)
        AuthEngineClass.BN_mul(ptr2, Me.BNk, Me.BNv, ptr5)
        AuthEngineClass.BN_add(ptr3, ptr1, ptr2)
        AuthEngineClass.BN_Mod(Me.BNPublicB, ptr3, Me.BNn, ptr5)
        AuthEngineClass.BN_bn2bin(Me.BNPublicB, Me.PublicB)
        Array.Reverse(Me.PublicB)
    End Sub
    Private Sub CalculateK()
        Dim algorithm1 As New SHA1Managed
        Dim list1 As New ArrayList
        list1 = AuthEngineClass.Split(Me.S)
        list1.Item(0) = algorithm1.ComputeHash(CType(list1.Item(0), Byte()))
        list1.Item(1) = algorithm1.ComputeHash(CType(list1.Item(1), Byte()))
        Me.SS_Hash = AuthEngineClass.Combine(CType(list1.Item(0), Byte()), CType(list1.Item(1), Byte()))
    End Sub
    Public Sub CalculateM2(ByVal m1 As Byte())
        Dim algorithm1 As New SHA1Managed
        Dim buffer1 As Byte() = New Byte(((Me.A.Length + m1.Length) + Me.SS_Hash.Length) - 1) {}
        Buffer.BlockCopy(Me.A, 0, buffer1, 0, Me.A.Length)
        Buffer.BlockCopy(m1, 0, buffer1, Me.A.Length, m1.Length)
        Buffer.BlockCopy(Me.SS_Hash, 0, buffer1, (Me.A.Length + m1.Length), Me.SS_Hash.Length)
        Me.M2 = algorithm1.ComputeHash(buffer1)
    End Sub
    Private Sub CalculateS()
        Dim ptr1 As IntPtr = AuthEngineClass.BN_New
        Dim ptr2 As IntPtr = AuthEngineClass.BN_New
        Dim ptr3 As IntPtr = AuthEngineClass.BN_New
        Dim ptr4 As IntPtr = AuthEngineClass.BN_New
        Me.BNS = AuthEngineClass.BN_New
        Dim ptr5 As IntPtr = AuthEngineClass.BN_ctx_new
        Me.S = New Byte(32 - 1) {}
        AuthEngineClass.BN_mod_exp(ptr1, Me.BNv, Me.BNU, Me.BNn, ptr5)
        AuthEngineClass.BN_mul(ptr2, Me.BNA, ptr1, ptr5)
        AuthEngineClass.BN_mod_exp(Me.BNS, ptr2, Me.BNb, Me.BNn, ptr5)
        AuthEngineClass.BN_bn2bin(Me.BNS, Me.S)
        Array.Reverse(Me.S)
        Me.CalculateK()
    End Sub
    Public Sub CalculateU(ByVal a As Byte())
        Me.A = a
        Dim algorithm1 As New SHA1Managed
        Dim buffer1 As Byte() = New Byte((a.Length + Me.PublicB.Length) - 1) {}
        Buffer.BlockCopy(a, 0, buffer1, 0, a.Length)
        Buffer.BlockCopy(Me.PublicB, 0, buffer1, a.Length, Me.PublicB.Length)
        Me.U = algorithm1.ComputeHash(buffer1)
        Array.Reverse(Me.U)
        Me.BNU = AuthEngineClass.BN_Bin2BN(Me.U, Me.U.Length, IntPtr.Zero)
        Array.Reverse(Me.U)
        Array.Reverse(Me.A)
        Me.BNA = AuthEngineClass.BN_Bin2BN(Me.A, Me.A.Length, IntPtr.Zero)
        Array.Reverse(Me.A)
        Me.CalculateS()
    End Sub
    Private Sub CalculateV()
        Me.BNv = AuthEngineClass.BN_New
        Dim ptr1 As IntPtr = AuthEngineClass.BN_ctx_new
        AuthEngineClass.BN_mod_exp(Me.BNv, Me.BNg, Me.BNx, Me.BNn, ptr1)
        Me.CalculateB()
    End Sub
    Public Sub CalculateX(ByVal username As Byte(), ByVal password As Byte())
        Me.Username = username
        Me.Password = password

        Dim buffer1 As Byte() = username
        Dim buffer2 As Byte() = password
        Dim algorithm1 As New SHA1Managed
        Dim encoding1 As New UTF7Encoding
        Dim buffer3 As Byte() = New Byte(20 - 1) {}
        Dim buffer4 As Byte() = New Byte(((buffer1.Length + buffer2.Length) + 1) - 1) {}
        Dim buffer5 As Byte() = New Byte((Me.salt.Length + 20) - 1) {}
        Buffer.BlockCopy(buffer1, 0, buffer4, 0, buffer1.Length)
        buffer4(buffer1.Length) = 58
        Buffer.BlockCopy(buffer2, 0, buffer4, (buffer1.Length + 1), buffer2.Length)
        Buffer.BlockCopy(algorithm1.ComputeHash(buffer4, 0, buffer4.Length), 0, buffer5, Me.salt.Length, 20)
        Buffer.BlockCopy(Me.salt, 0, buffer5, 0, Me.salt.Length)
        buffer3 = algorithm1.ComputeHash(buffer5)
        Array.Reverse(buffer3)
        Me.BNx = AuthEngineClass.BN_Bin2BN(buffer3, buffer3.Length, IntPtr.Zero)
        Array.Reverse(Me.g)
        Me.BNg = AuthEngineClass.BN_Bin2BN(Me.g, Me.g.Length, IntPtr.Zero)
        Array.Reverse(Me.g)
        Array.Reverse(Me.k)
        Me.BNk = AuthEngineClass.BN_Bin2BN(Me.k, Me.k.Length, IntPtr.Zero)
        Array.Reverse(Me.k)
        Array.Reverse(Me.N)
        Me.BNn = AuthEngineClass.BN_Bin2BN(Me.N, Me.N.Length, IntPtr.Zero)
        Array.Reverse(Me.N)
        Me.CalculateV()
    End Sub

    Public Sub CalculateM1()
        Dim algorithm1 As New SHA1Managed
        Dim N_Hash As Byte() = New Byte(20 - 1) {}
        Dim G_Hash As Byte() = New Byte(20 - 1) {}
        Dim NG_Hash As Byte() = New Byte(20 - 1) {}
        Dim User_Hash As Byte() = New Byte(20 - 1) {}
        Dim i As Integer

        N_Hash = algorithm1.ComputeHash(N)
        G_Hash = algorithm1.ComputeHash(g)
        User_Hash = algorithm1.ComputeHash(Username)
        For i = 0 To 19
            NG_Hash(i) = CType(N_Hash(i) Xor G_Hash(i), Byte)
        Next i

        Dim temp As Byte() = AuthEngineClass.Concat(NG_Hash, User_Hash)
        temp = AuthEngineClass.Concat(temp, Me.salt)
        temp = AuthEngineClass.Concat(temp, Me.A)
        temp = AuthEngineClass.Concat(temp, Me.PublicB)
        temp = AuthEngineClass.Concat(temp, Me.SS_Hash)
        Me.M1 = algorithm1.ComputeHash(temp)
    End Sub
    Public Sub CalculateM1_Full()
        Dim sha2 As New SHA1CryptoServiceProvider
        Dim i As Byte = 0

        'Calc S1/S2
        Dim S1 As Byte() = New Byte(16 - 1) {}
        Dim S2 As Byte() = New Byte(16 - 1) {}
        Do While (i < 16)
            S1(i) = S((i * 2))
            S2(i) = S(((i * 2) + 1))
            i += 1
        Loop

        'Calc SSHash
        Dim S1_Hash As Byte() = sha2.ComputeHash(S1)
        Dim S2_Hash As Byte() = sha2.ComputeHash(S2)
        ReDim SS_Hash(32 - 1)
        i = 0
        Do While (i < 16)
            Me.SS_Hash((i * 2)) = S1_Hash(i)
            Me.SS_Hash(((i * 2) + 1)) = S2_Hash(i)
            i += 1
        Loop

        'Calc M1
        Dim N_Hash As Byte() = sha2.ComputeHash(Me.N)
        Dim G_Hash As Byte() = sha2.ComputeHash(Me.g)
        Dim User_Hash As Byte() = sha2.ComputeHash(Me.Username)

        Dim NG_Hash As Byte() = New Byte(20 - 1) {}
        i = 0
        Do While (i < 20)
            NG_Hash(i) = CType((N_Hash(i) Xor G_Hash(i)), Byte)
            i += 1
        Loop

        Dim temp As Byte() = AuthEngineClass.Concat(NG_Hash, User_Hash)
        temp = AuthEngineClass.Concat(temp, Me.salt)
        temp = AuthEngineClass.Concat(temp, Me.A)
        temp = AuthEngineClass.Concat(temp, Me.PublicB)
        temp = AuthEngineClass.Concat(temp, Me.SS_Hash)
        Me.M1 = sha2.ComputeHash(temp)
    End Sub

    Public Sub CalculateTrafficKey()
        Dim sha1 As New SHA1Managed
        Dim i As Integer
        Dim opad As Byte() = New Byte(64 - 1) {}
        Dim ipad As Byte() = New Byte(64 - 1) {}

        'Static 16 byte Key located at 0x0088FB3C
        Dim Key As Byte() = {&H38, &HA7, &H83, &H15, &HF8, &H92, &H25, &H30, &H71, &H98, &H67, &HB1, &H8C, &H4, &HE2, &HAA}

        'Fill 64 bytes of same value
        For i = 0 To 64 - 1
            opad(i) = &H5C
            ipad(i) = &H36
        Next

        'XOR Values
        For i = 0 To 16 - 1
            opad(i) = opad(i) Xor Key(i)
            ipad(i) = ipad(i) Xor Key(i)
        Next

        Dim buffer1() As Byte
        Dim buffer2() As Byte

        buffer1 = Concat(ipad, SS_Hash)
        buffer2 = sha1.ComputeHash(buffer1)

        buffer1 = Concat(opad, buffer2)
        SS_Hash = sha1.ComputeHash(buffer1)
    End Sub

    Public Sub CalculateCRCHash()
        Dim SHA1 As New SHA1Managed
        Dim Buffer1(63) As Byte, Buffer2(63) As Byte
        Dim i As Integer

        'DONE: Check if the
        If IO.Directory.Exists("crc") Then
            For i = 0 To 63
                Buffer1(i) = &H36
                Buffer2(i) = &H5C
            Next

            For i = 0 To CrcSalt.Length - 1
                Buffer1(i) = Buffer1(i) Xor CrcSalt(i)
                Buffer2(i) = Buffer2(i) Xor CrcSalt(i)
            Next

            'DONE: Three files are used (WoW.exe, DivxDecoder.dll, unicows.dll)
            'NOTE: WoW.exe and DivxDecoder.dll are loaded from pieces of data, while Unicows.dll is normal full file loading.
            'NOTE: It seems like every language use the exact same files. There is a function inside the exe that checks what region it is. But only europe and usa was on that list so china etc might use different files.
            Dim Files() As String = {"WoW.bin", "DivxDecoder.bin", "Unicows.bin"}

            Dim fs As FileStream
            For Each File As String In Files
                If IO.File.Exists("crc\" & File) Then
                    fs = New FileStream("crc\" & File, FileMode.Open, FileAccess.Read)
                    Dim FileBuffer(fs.Length - 1) As Byte
                    fs.Read(FileBuffer, 0, fs.Length)
                    Buffer1 = AuthEngineClass.Concat(Buffer1, FileBuffer)
                    FileBuffer = Nothing

                    fs.Close()
                    fs = Nothing
                Else
                    Console.ForegroundColor = System.ConsoleColor.Red
                    Console.WriteLine("[{0}] CRC File '{1}' missing", Format(TimeOfDay, "hh:mm:ss"), File)
                    Console.ForegroundColor = System.ConsoleColor.Gray
                End If
            Next

            Dim Hash1() As Byte = SHA1.ComputeHash(Buffer1)
            Buffer2 = AuthEngineClass.Concat(Buffer2, Hash1)
            Dim Hash2() As Byte = SHA1.ComputeHash(Buffer2)

            Dim Buffer3() As Byte
            Buffer3 = AuthEngineClass.Concat(A, Hash2)
            CrcHash = SHA1.ComputeHash(Buffer3)

            'DONE: Clear up
            Buffer1 = Nothing
            Buffer2 = Nothing
            Buffer3 = Nothing
            Hash1 = Nothing
            Hash2 = Nothing
        Else
            Console.ForegroundColor = System.ConsoleColor.Red
            Console.WriteLine("[{0}] CRC Files missing", Format(TimeOfDay, "hh:mm:ss"))
            Console.ForegroundColor = System.ConsoleColor.Gray
            CrcHash = New Byte(19) {}
        End If
    End Sub
#End Region

#Region "AuthEngine.Functions"
    Private Shared Function Combine(ByVal Bytes1 As Byte(), ByVal Bytes2 As Byte()) As Byte()
        If (Bytes1.Length <> Bytes2.Length) Then Return Nothing

        Dim CombineBuffer As Byte() = New Byte(Bytes1.Length + Bytes2.Length - 1) {}
        Dim Counter As Integer = 0

        For i As Integer = 0 To CombineBuffer.Length - 1 Step 2
            CombineBuffer(i) = Bytes1(Counter)
            Counter += 1
        Next
        Counter = 0

        For i As Integer = 1 To CombineBuffer.Length - 1 Step 2
            CombineBuffer(i) = Bytes2(Counter)
            Counter += 1
        Next

        Return CombineBuffer

    End Function

    Public Shared Function Concat(ByVal Buffer1 As Byte(), ByVal Buffer2 As Byte()) As Byte()
        Dim ConcatBuffer As Byte() = New Byte(Buffer1.Length + Buffer2.Length - 1) {}
        Array.Copy(Buffer1, ConcatBuffer, Buffer1.Length)
        Array.Copy(Buffer2, 0, ConcatBuffer, Buffer1.Length, Buffer2.Length)
        Return ConcatBuffer

    End Function
    <DllImport("LIBEAY32.DLL")> _
    Public Shared Function RAND_bytes(ByVal buf As Byte(), ByVal num As Integer) As Integer

    End Function

    Private Shared Function Split(ByVal ByteBuffer As Byte()) As ArrayList

        Dim SplitBuffer1 As Byte() = New Byte(ByteBuffer.Length / 2 - 1) {}
        Dim SplitBuffer2 As Byte() = New Byte(ByteBuffer.Length / 2 - 1) {}
        Dim ReturnList As New ArrayList
        Dim Counter As Integer = 0

        For i As Integer = 0 To SplitBuffer1.Length - 1
            SplitBuffer1(i) = ByteBuffer(Counter)
            Counter += 2
        Next

        Counter = 1

        For i As Integer = 0 To SplitBuffer2.Length - 1
            SplitBuffer2(i) = ByteBuffer(Counter)
            Counter += 2
        Next


        ReturnList.Add(SplitBuffer1)
        ReturnList.Add(SplitBuffer2)

        Return ReturnList
    End Function
#End Region

#Region "AuthEngine.Variables"

    Private A As Byte()
    Private b As Byte()
    Public PublicB As Byte()
    Public g As Byte()
    Private k As Byte()
    'Private PublicK As Byte() = SS_Hash
    Public M2 As Byte()
    Public N As Byte()
    Private Password As Byte()
    Private S As Byte()
    Public salt As Byte()
    Private U As Byte()
    Public Shared CrcSalt As Byte()
    Public CrcHash As Byte()
    Public Username As Byte()

    Public M1 As Byte()
    Public SS_Hash As Byte()
#End Region

#Region "AuthEngine.BigIntegers"
    Private BNA As IntPtr
    Private BNb As IntPtr
    Private BNPublicB As IntPtr
    Private BNg As IntPtr
    Private BNk As IntPtr
    Private BNn As IntPtr
    Private BNS As IntPtr
    Private BNU As IntPtr
    Private BNv As IntPtr
    Private BNx As IntPtr
#End Region

End Class
