﻿'=============================================================================
'                  Crypter.NET - FUD Runtime crypter 
'                 Copyright (C) 2010 fLaSh - Carlos.DF 
'                        <c4rl0s.pt@gmail.com> 
'                     <http://www.flash1337.com/>
' THIS SOFTWARE IS PROVIDED BY THE AUTHORS ``AS IS'' AND ANY EXPRESS OR
' IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
' OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
' IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY DIRECT, INDIRECT,
' INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
' NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
' DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
' THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
' (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
' THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
'=============================================================================
Imports System
Imports System.Collections
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Public Class WaterFX
    Inherits System.Windows.Forms.Panel

    Private effectTimer As System.Windows.Forms.Timer
    Private tmrBalance As System.Windows.Forms.Timer
    Private components As System.ComponentModel.IContainer

    Private _bmp As Bitmap
    Private _waves As Short(,,)
    Private _waveWidth As Integer
    Private _waveHeight As Integer
    Private _activeBuffer As Integer = 0
    Private _weHaveWaves As Boolean
    Private _bmpHeight As Integer, _bmpWidth As Integer
    Private _bmpBytes As Byte()
    Private _bmpBitmapData As BitmapData
    Private _scale As Integer

    Private __IsBusy As Boolean

    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.effectTimer = New System.Windows.Forms.Timer(Me.components)
        Me.tmrBalance = New System.Windows.Forms.Timer(Me.components)
        ' 
        ' effectTimer
        ' 
        AddHandler Me.effectTimer.Tick, AddressOf Me.effectTimer_Tick
        AddHandler Me.tmrBalance.Tick, AddressOf Me.tmrBalance_Tick

        ' 
        ' WaterEffectControl
        ' 
        AddHandler Me.Paint, AddressOf Me.WaterEffectControl_Paint
        AddHandler Me.MouseMove, AddressOf Me.WaterEffectControl_MouseMove

    End Sub

    Public Sub New()
        InitializeComponent()
        effectTimer.Enabled = True
        effectTimer.Interval = 100
        tmrBalance.Interval = 1000
        SetStyle(ControlStyles.UserPaint, True)
        SetStyle(ControlStyles.AllPaintingInWmPaint, True)
        SetStyle(ControlStyles.DoubleBuffer, True)
        Me.BackColor = Color.Transparent
        _weHaveWaves = False
        _scale = 1
    End Sub

    Public Sub New(ByVal bmp As Bitmap)
        Me.New()
        Me.ImageBitmap = bmp
    End Sub

    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        '_bmp.UnlockBits(_bmpBitmapData)
        If disposing Then
            If components IsNot Nothing Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    ''' <summary>
    ''' Timer handler
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub effectTimer_Tick(ByVal sender As Object, ByVal e As System.EventArgs)
        If _weHaveWaves Then
            Invalidate()
            ProcessWaves()
        End If
    End Sub
    Private Sub tmrBalance_Tick(ByVal sender As Object, ByVal e As System.EventArgs)
        __IsBusy = Not __IsBusy
    End Sub

    ''' <summary>
    ''' Paint handler
    ''' 
    ''' Calculates the final effect-image out of
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Public Sub WaterEffectControl_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs)

        If IsNothing(_bmp) Then Return
        Dim tmp As Bitmap = Nothing

        On Error Resume Next

        tmp = DirectCast(_bmp.Clone(), Bitmap)
        Dim xOffset As Integer, yOffset As Integer
        Dim alpha As Byte

        If _weHaveWaves Then
            Dim tmpData As BitmapData = tmp.LockBits(New Rectangle(0, 0, _bmpWidth, _bmpHeight), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)

            Dim tmpBytes As Byte() = New Byte(_bmpWidth * _bmpHeight * 4 - 1) {}

            Marshal.Copy(tmpData.Scan0, tmpBytes, 0, _bmpWidth * _bmpHeight * 4)

            For x As Integer = 1 To _bmpWidth - 2
                For y As Integer = 1 To _bmpHeight - 2
                    Dim waveX As Integer = CInt(x) >> _scale
                    Dim waveY As Integer = CInt(y) >> _scale

                    'check bounds
                    If waveX <= 0 Then
                        waveX = 1
                    End If
                    If waveY <= 0 Then
                        waveY = 1
                    End If
                    If waveX >= _waveWidth - 1 Then
                        waveX = _waveWidth - 2
                    End If
                    If waveY >= _waveHeight - 1 Then
                        waveY = _waveHeight - 2
                    End If

                    'this gives us the effect of water breaking the light
                    xOffset = (_waves(waveX - 1, waveY, _activeBuffer) - _waves(waveX + 1, waveY, _activeBuffer)) >> 3
                    yOffset = (_waves(waveX, waveY - 1, _activeBuffer) - _waves(waveX, waveY + 1, _activeBuffer)) >> 3

                    If (xOffset <> 0) OrElse (yOffset <> 0) Then
                        'check bounds
                        If x + xOffset >= _bmpWidth - 1 Then
                            xOffset = _bmpWidth - x - 1
                        End If
                        If y + yOffset >= _bmpHeight - 1 Then
                            yOffset = _bmpHeight - y - 1
                        End If
                        If x + xOffset < 0 Then
                            xOffset = -x
                        End If
                        If y + yOffset < 0 Then
                            yOffset = -y
                        End If
                        If xOffset <= 0 Then xOffset = 0
                        'generate alpha
                        alpha = CByte(200 - xOffset)
                        If alpha < 0 Then
                            alpha = 0
                        End If
                        If alpha > 255 Then
                            alpha = 254
                        End If

                        'set colors
                        tmpBytes(4 * (x + y * _bmpWidth)) = _bmpBytes(4 * (x + xOffset + (y + yOffset) * _bmpWidth))
                        tmpBytes(4 * (x + y * _bmpWidth) + 1) = _bmpBytes(4 * (x + xOffset + (y + yOffset) * _bmpWidth) + 1)
                        tmpBytes(4 * (x + y * _bmpWidth) + 2) = _bmpBytes(4 * (x + xOffset + (y + yOffset) * _bmpWidth) + 2)
                        tmpBytes(4 * (x + y * _bmpWidth) + 3) = alpha
 
                    End If

                Next
                If Not Err.Number = 0 Then Exit For

            Next

            'copy data back
            Marshal.Copy(tmpBytes, 0, tmpData.Scan0, _bmpWidth * _bmpHeight * 4)
            tmp.UnlockBits(tmpData)

        End If

        e.Graphics.DrawImage(tmp, 0, 0, Me.ClientRectangle.Width, Me.ClientRectangle.Height)

        If Not Err.Number = 0 Then Debug.WriteLine("WaterEffectControl_Paint: " & Err.Description)

        If Not IsNothing(tmp) Then tmp.Dispose()

    End Sub

    ''' <summary>
    ''' This is the method that actually does move the waves around and simulates the
    ''' behaviour of water.
    ''' </summary>
    Private Sub ProcessWaves()

        Dim newBuffer As Integer = If((_activeBuffer = 0), 1, 0)
        Dim wavesFound As Boolean = False
        If newBuffer < 0 Then newBuffer = 1

        On Error Resume Next
        For x As Integer = 1 To _waveWidth - 2
            For y As Integer = 1 To _waveHeight - 2
                _waves(x, y, newBuffer) = CShort((((_waves(x - 1, y - 1, _activeBuffer) + _waves(x, y - 1, _activeBuffer) + _waves(x + 1, y - 1, _activeBuffer) + _waves(x - 1, y, _activeBuffer) + _waves(x + 1, y, _activeBuffer) + _waves(x - 1, y + 1, _activeBuffer) + _waves(x, y + 1, _activeBuffer) + _waves(x + 1, y + 1, _activeBuffer)) >> 2) - _waves(x, y, newBuffer)))
                'damping
                If _waves(x, y, newBuffer) <> 0 Then
                    _waves(x, y, newBuffer) -= CShort((_waves(x, y, newBuffer) >> 4))
                    wavesFound = True
                End If
                If Not Err.Number = 0 Then Exit For
            Next
            If Not Err.Number = 0 Then Exit For
        Next

        _weHaveWaves = wavesFound
        _activeBuffer = newBuffer

    End Sub


    ''' <summary>
    ''' This function is used to start a wave by simulating a round drop
    ''' </summary>
    ''' <param name="x">x position of the drop</param>
    ''' <param name="y">y position of the drop</param>
    ''' <param name="height">Height position of the drop</param>
    Private Sub PutDrop(ByVal x As Integer, ByVal y As Integer, ByVal height As Short)
        _weHaveWaves = True
        Dim radius As Integer = 20
        Dim dist As Double
        On Error Resume Next
        For i As Integer = -radius To radius
            For j As Integer = -radius To radius
                If ((x + i >= 0) AndAlso (x + i < _waveWidth - 1)) AndAlso ((y + j >= 0) AndAlso (y + j < _waveHeight - 1)) Then
                    dist = Math.Sqrt(i * i + j * j)
                    If dist < radius Then
                        _waves(x + i, y + j, _activeBuffer) = CShort((Math.Cos(dist * Math.PI / radius) * height))
                    End If
                End If
                If Not Err.Number = 0 Then Return
            Next
            If Not Err.Number = 0 Then Return
        Next
    End Sub

    ''' <summary>
    ''' The MouseMove handler.
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub WaterEffectControl_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        ' If e.Button = MouseButtons.Left Then
        On Error Resume Next
        If Not __IsBusy Then
            Dim realX As Integer = CInt(((e.X / CDbl(Me.ClientRectangle.Width)) * _waveWidth))
            Dim realY As Integer = CInt(((e.Y / CDbl(Me.ClientRectangle.Height)) * _waveHeight))
            If Not Err.Number = 0 Then Return
            PutDrop(realX, realY, 200)
        End If
        If Not tmrBalance.Enabled Then tmrBalance.Start()
    End Sub

#Region "Properties"
    ''' <summary>
    ''' Our background image
    ''' </summary>
    Public Property ImageBitmap() As Bitmap
        Get
            Return _bmp
        End Get
        Set(ByVal value As Bitmap)
            _bmp = value
            If IsNothing(_bmp) Then
                effectTimer.Stop()
                tmrBalance.Stop()
                Return
            Else
                effectTimer.Start()
                __IsBusy = False
            End If

            _bmpHeight = _bmp.Height
            _bmpWidth = _bmp.Width

            _waveWidth = _bmpWidth >> _scale
            _waveHeight = _bmpHeight >> _scale
            _waves = New Int16(_waveWidth - 1, _waveHeight - 1, 1) {}

            _bmpBytes = New Byte(_bmpWidth * _bmpHeight * 4 - 1) {}
            _bmpBitmapData = _bmp.LockBits(New Rectangle(0, 0, _bmpWidth, _bmpHeight), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            Marshal.Copy(_bmpBitmapData.Scan0, _bmpBytes, 0, _bmpWidth * _bmpHeight * 4)
        End Set
    End Property

    ''' <summary>
    ''' The scale of the wave matrix compared to the size of the image.
    ''' Use it for large images to reduce processor load.
    ''' 
    ''' 0 : wave resolution is the same than image resolution
    ''' 1 : wave resolution is half the image resolution
    ''' ...and so on
    ''' </summary>
    Public Shadows Property Scale() As Integer
        Get
            Return _scale
        End Get
        Set(ByVal value As Integer)
            _scale = value
        End Set
    End Property

#End Region
End Class

