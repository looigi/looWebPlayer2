Imports System.IO
Imports System.Text
Imports System.Security.AccessControl
Imports System.Runtime.InteropServices
Imports System.ComponentModel

Public Structure ModalitaDiScan
    Dim TipologiaScan As Integer
    Const SoloStruttura = 0
    Const Elimina = 1
End Structure

Public Class GestioneFilesDirectory
    Private barra As String = "\"

    Private DirectoryRilevate() As String
    Private FilesRilevati() As String
    Private QuantiFilesRilevati As Long
    Private QuanteDirRilevate As Long
    Private RootDir As String
    Private Eliminati As Boolean
    Private Percorso As String

    Public Const NonEliminareRoot As Boolean = False
    Public Const EliminaRoot As Boolean = True
    Public Const NonEliminareFiles As Boolean = False
    Public Const EliminaFiles As Boolean = True

    Private DimensioniArrayAttualeDir As Long
    Private DimensioniArrayAttualeFiles As Long

    Public Sub PrendeRoot(R As String)
        RootDir = R
    End Sub

    Public Function RitornaFilesRilevati() As String()
        Return FilesRilevati
    End Function

    Public Function RitornaDirectoryRilevate() As String()
        Return DirectoryRilevate
    End Function

    Public Function RitornaQuantiFilesRilevati() As Long
        Return QuantiFilesRilevati
    End Function

    Public Function RitornaQuanteDirectoryRilevate() As Long
        Return QuanteDirRilevate
    End Function

    Public Sub ImpostaPercorsoAttuale(sPercorso As String)
        Percorso = sPercorso
    End Sub

    Public Function TornaDimensioneFile(NomeFile As String) As Long
        If File.Exists(NomeFile) Then
            Dim infoReader As System.IO.FileInfo
            infoReader = My.Computer.FileSystem.GetFileInfo(NomeFile)
            Dim Dime As Long = infoReader.Length
            infoReader = Nothing

            Return Dime
        Else
            Return -1
        End If
    End Function

    Public Sub PulisceCartelleVuote(Percorso As String)
        Dim qFiles As Integer
        ScansionaDirectorySingola(Percorso)
        Dim Direct() As String = RitornaDirectoryRilevate()
        Dim qDir As Integer = RitornaQuanteDirectoryRilevate()

        For i As Integer = qDir To 1 Step -1
            ScansionaDirectorySingola(Direct(i))
            qFiles = RitornaQuantiFilesRilevati()
            If qFiles = 0 Then
                RmDir(Direct(i))
            End If
        Next
    End Sub

    Public Function NomeFileEsistente(NomeFile As String) As String
        Dim NomeFileDestinazione As String = NomeFile
        Dim gf As New GestioneFilesDirectory
        Dim Estensione As String = gf.TornaEstensioneFileDaPath(NomeFileDestinazione)
        If Estensione <> "" Then
            NomeFileDestinazione = NomeFileDestinazione.Replace(Estensione, "")
        End If
        Dim Contatore As Integer = 1

        Do While File.Exists(NomeFileDestinazione & "_" & Format(Contatore, "0000") & Estensione) = True
            Contatore += 1
        Loop

        NomeFileDestinazione = NomeFileDestinazione & "_" & Format(Contatore, "0000") & Estensione
        gf = Nothing

        Return NomeFileDestinazione
    End Function

    Public Function EliminaFileFisico(NomeFileOrigine As String) As String
        If File.Exists(NomeFileOrigine) Then
            Dim Ritorno As String = ""

            If NomeFileOrigine.Trim <> "" Then
                Try
                    File.Delete(NomeFileOrigine)

                    Do While (System.IO.File.Exists(NomeFileOrigine) = True)
                        Threading.Thread.Sleep(1000)
                    Loop
                Catch ex As Exception
                    Ritorno = "ERRORE: " & ex.Message
                End Try
            End If

            Return Ritorno
        Else
            Return ""
        End If
    End Function

    Public Function PrendeAttributiFile(Filetto As String) As FileAttribute
        If File.Exists(Filetto) Then
            Dim attributes As FileAttributes
            attributes = File.GetAttributes(Filetto)

            Return attributes
        Else
            Return Nothing
        End If
    End Function

    Public Sub ImpostaAttributiFile(Filetto As String, Attributi As FileAttribute)
        If File.Exists(Filetto) Then
            File.SetAttributes(Filetto, Attributi)
        End If
    End Sub

    Public Function CopiaFileFisico(NomeFileOrigine As String, NomeFileDestinazione As String, SovraScrittura As Boolean) As String
        Dim Ritorno As String = ""

        If File.Exists(NomeFileOrigine) Then
            If NomeFileOrigine.Trim <> "" And NomeFileDestinazione.Trim <> "" And NomeFileOrigine.Trim.ToUpper <> NomeFileDestinazione.Trim.ToUpper Then
                Dim Ok As Boolean = True

                If File.Exists(NomeFileDestinazione) Then
                    If SovraScrittura = False Then
                        NomeFileDestinazione = NomeFileEsistente(NomeFileDestinazione)
                    Else
                        If FileLen(NomeFileOrigine) = FileLen(NomeFileDestinazione) And Math.Abs(DateDiff(DateInterval.Second, FileDateTime(NomeFileOrigine), FileDateTime(NomeFileDestinazione))) < 60 Then
                            Ritorno = "SKIPPED"
                            Ok = False
                        End If
                    End If
                End If

                If Ok Then
                    Dim dataUltimoAccesso As Date = TornaDataUltimoAccesso(NomeFileOrigine)
                    Dim attr As FileAttribute = PrendeAttributiFile(NomeFileOrigine)
                    ImpostaAttributiFile(NomeFileOrigine, FileAttribute.Normal)

                    Try
                        File.Copy(NomeFileOrigine, NomeFileDestinazione, True)

                        Do Until (System.IO.File.Exists(NomeFileDestinazione))
                            Threading.Thread.Sleep(1000)
                        Loop

                        ImpostaAttributiFile(NomeFileDestinazione, attr)
                        Ritorno = TornaNomeFileDaPath(NomeFileDestinazione)
                    Catch ex As Exception
                        Ritorno = "ERRORE: " & ex.Message
                    End Try

                    ImpostaAttributiFile(NomeFileOrigine, attr)
                    File.SetLastAccessTime(NomeFileOrigine, dataUltimoAccesso)
                End If
            End If

            Return Ritorno
        Else
            Return "ERRORE: File di origine non presente"
        End If
    End Function

    Public Function TornaNomeFileDaPath(Percorso As String) As String
        Dim Ritorno As String = ""

        For i As Integer = Percorso.Length To 1 Step -1
            If Mid(Percorso, i, 1) = "/" Or Mid(Percorso, i, 1) = barra Then
                Ritorno = Mid(Percorso, i + 1, Percorso.Length)
                Exit For
            End If
        Next

        Return Ritorno
    End Function

    Public Function TornaEstensioneFileDaPath(Percorso As String) As String
        Dim Ritorno As String = ""

        For i As Integer = Percorso.Length To 1 Step -1
            If Mid(Percorso, i, 1) = "." Then
                Ritorno = Mid(Percorso, i, Percorso.Length)
                Exit For
            End If
        Next
        If Ritorno.Length > 5 Then
            Ritorno = ""
        End If

        Return Ritorno
    End Function

    Public Function TornaNomeDirectoryDaPath(Percorso As String) As String
        Dim Ritorno As String = ""

        For i As Integer = Percorso.Length To 1 Step -1
            If Mid(Percorso, i, 1) = "/" Or Mid(Percorso, i, 1) = barra Then
                Ritorno = Mid(Percorso, 1, i - 1)
                Exit For
            End If
        Next

        Return Ritorno
    End Function

    Public Sub CreaAggiornaFile(NomeFile As String, Cosa As String)
        Try
            Dim path As String

            If Percorso <> "" Then
                path = Percorso & barra & NomeFile
            Else
                path = NomeFile
            End If

            path = path.Replace(barra & barra, barra)

            ' Create or overwrite the file.
            Dim fs As FileStream = File.Create(path)

            ' Add text to the file.
            Dim info As Byte() = New UTF8Encoding(True).GetBytes(Cosa)
            fs.Write(info, 0, info.Length)
            fs.Close()
        Catch ex As Exception
            'Dim StringaPassaggio As String
            'Dim H As HttpApplication = HttpContext.Current.ApplicationInstance

            'StringaPassaggio = "?Errore=Errore CreaAggiornaFileVisMese: " & Err.Description.Replace(" ", "%20").Replace(vbCrLf, "")
            'StringaPassaggio = StringaPassaggio & "&Utente=" & H.Session("Nick")
            'StringaPassaggio = StringaPassaggio & "&Chiamante=" & H.Request.CurrentExecutionFilePath.ToUpper.Trim
            'H.Response.Redirect("Errore.aspx" & StringaPassaggio)
        End Try
    End Sub

    Private objReader As StreamReader

    Public Sub ApreFilePerLettura(NomeFile As String)
        objReader = New StreamReader(NomeFile)
    End Sub

    Public Function RitornaRiga() As String
        Return objReader.ReadLine()
    End Function

    Public Sub ChiudeFile()
        objReader.Close()
    End Sub

    Public Function LeggeFileIntero(NomeFile As String) As String
        If File.Exists(NomeFile) Then
            Dim objReader As StreamReader = New StreamReader(NomeFile)
            Dim sLine As String = ""
            Dim Ritorno As String = ""

            Do
                sLine = objReader.ReadLine()
                Ritorno += sLine
            Loop Until sLine Is Nothing
            objReader.Close()

            Return Ritorno
        Else
            Return ""
        End If
    End Function

    Public Sub ScansionaDirectorySingola(Percorso As String, Optional Filtro As String = "", Optional lblAggiornamento As Label = Nothing)
        Eliminati = False

        PulisceInfo()

        QuanteDirRilevate += 1
        DirectoryRilevate(QuanteDirRilevate) = Percorso

        LeggeFilesDaDirectory(Percorso, Filtro)

        LeggeTutto(Percorso, Filtro, lblAggiornamento)
    End Sub

    Dim Conta As Integer

    Private Sub LeggeTutto(Percorso As String, Filtro As String, lblAggiornamento As Label)
        If Directory.Exists(Percorso) Then
            Dim di As New IO.DirectoryInfo(Percorso)
            Dim diar1 As IO.DirectoryInfo() = di.GetDirectories
            Dim dra As IO.DirectoryInfo

            For Each dra In diar1
                If lblAggiornamento Is Nothing = False Then
                    Conta += 1
                    If Conta = 2 Then
                        Conta = 0
                    End If
                End If

                QuanteDirRilevate += 1
                If QuanteDirRilevate > DimensioniArrayAttualeDir Then
                    DimensioniArrayAttualeDir += 10000
                    ReDim Preserve DirectoryRilevate(DimensioniArrayAttualeDir)
                End If
                DirectoryRilevate(QuanteDirRilevate) = dra.FullName

                LeggeFilesDaDirectory(dra.FullName, Filtro)

                LeggeTutto(dra.FullName, Filtro, lblAggiornamento)
            Next
        End If
    End Sub

    Public Sub PulisceInfo()
        Erase FilesRilevati
        QuantiFilesRilevati = 0
        Erase DirectoryRilevate
        QuanteDirRilevate = 0

        DimensioniArrayAttualeDir = 10000
        DimensioniArrayAttualeFiles = 10000

        ReDim DirectoryRilevate(DimensioniArrayAttualeDir)
        ReDim FilesRilevati(DimensioniArrayAttualeFiles)
    End Sub

    Public Function RitornaEliminati() As Boolean
        Return Eliminati
    End Function

    Public Sub LeggeFilesDaDirectory(Percorso As String, Optional Filtro As String = "")
        If Directory.Exists(Percorso) Then
            Dim di As New IO.DirectoryInfo(Percorso)

            Dim fi As New IO.DirectoryInfo(Percorso)
            Dim fiar1 As IO.FileInfo() = di.GetFiles
            Dim fra As IO.FileInfo
            Dim Ok As Boolean = True
            Dim Filtri() As String = Filtro.Split(";")

            For Each fra In fiar1
                Ok = False
                If Filtro <> "" Then
                    For i As Integer = 0 To Filtri.Length - 1
                        If fra.FullName.ToUpper.IndexOf(Filtri(i).ToUpper.Trim.Replace("*", "")) > -1 Then
                            Ok = True
                            Exit For
                        End If
                    Next
                Else
                    Ok = True
                End If
                If Ok = True Then
                    QuantiFilesRilevati += 1
                    If QuantiFilesRilevati > DimensioniArrayAttualeFiles Then
                        DimensioniArrayAttualeFiles += 10000
                        ReDim Preserve FilesRilevati(DimensioniArrayAttualeFiles)
                    End If
                    FilesRilevati(QuantiFilesRilevati) = fra.FullName
                End If
            Next
        End If
    End Sub

    Public Sub CreaDirectoryDaPercorso(Percorso As String)
        Dim Ritorno As String = Percorso

        For i As Integer = 1 To Ritorno.Length
            If Mid(Ritorno, i, 1) = barra Then
                On Error Resume Next
                MkDir(Mid(Ritorno, 1, i))
                On Error GoTo 0
            End If
        Next
    End Sub

    Public Function Ordina(Filetti() As String) As String()
        If Filetti Is Nothing Then
            Return Nothing
            Exit Function
        End If

        Dim Appoggio() As String = Filetti
        Dim Appo As String

        For i As Integer = 1 To QuantiFilesRilevati
            If Appoggio(i) <> "" Then
                For k As Integer = i + 1 To QuanteDirRilevate
                    If Appoggio(k) <> "" Then
                        If Appoggio(i).ToUpper.Trim > Appoggio(k).ToUpper.Trim Then
                            Appo = Appoggio(i)
                            Appoggio(i) = Appoggio(k)
                            Appoggio(k) = Appo
                        End If
                    End If
                Next
            End If
        Next

        Return Appoggio
    End Function

    Public Sub EliminaAlberoDirectory(Percorso As String, EliminaRoot As Boolean, EliminaFiles As Boolean)
        If Directory.Exists(Percorso) Then
            ScansionaDirectorySingola(Percorso, "")

            If DirectoryRilevate Is Nothing = False Then
                DirectoryRilevate = Ordina(DirectoryRilevate)
            End If

            If EliminaFiles = True Then
                FilesRilevati = Ordina(FilesRilevati)

                For i As Integer = QuantiFilesRilevati To 1 Step -1
                    Try
                        EliminaFileFisico(FilesRilevati(i))
                    Catch ex As Exception

                    End Try
                Next
            End If

            For i As Integer = QuanteDirRilevate To 1 Step -1
                Try
                    RmDir(DirectoryRilevate(i))
                Catch ex As Exception

                End Try
            Next

            If EliminaRoot = True Then
                Try
                    RmDir(Percorso)
                Catch ex As Exception

                End Try
            End If
        End If
    End Sub

    Public Function TornaDataDiCreazione(NomeFile As String) As Date
        If File.Exists(NomeFile) Then
            Dim info As New FileInfo(NomeFile)
            Return info.CreationTime
        Else
            Return Nothing
        End If
    End Function

    Public Function TornaDataDiUltimaModifica(NomeFile As String) As Date
        If File.Exists(NomeFile) Then
            Dim info As New FileInfo(NomeFile)
            Return info.LastWriteTime
        Else
            Return Nothing
        End If
    End Function

    Public Function TornaDataUltimoAccesso(NomeFile As String) As Date
        If File.Exists(NomeFile) Then
            Dim info As New FileInfo(NomeFile)
            Return info.LastAccessTime
        Else
            Return Nothing
        End If
    End Function

    Private outputFile As StreamWriter

    Public Sub ApreFileDiTestoPerScrittura(Percorso As String)
        outputFile = New StreamWriter(Percorso, True)
    End Sub

    Public Sub ScriveTestoSuFileAperto(Cosa As String)
        outputFile.WriteLine(Cosa)
    End Sub

    Public Sub ChiudeFileDiTestoDopoScrittura()
        outputFile.Flush()
        outputFile.Close()
    End Sub

    Public Sub LockCartella(Cartella As String)
        Try
            Dim fs As FileSystemSecurity = File.GetAccessControl(Cartella)
            fs.AddAccessRule(New FileSystemAccessRule(Environment.UserName, FileSystemRights.FullControl, AccessControlType.Deny))
            File.SetAccessControl(Cartella, fs)
        Catch ex As Exception

        End Try
    End Sub

    Public Sub UnLockCartella(Cartella As String)
        Try
            Dim fs As FileSystemSecurity = File.GetAccessControl(Cartella)
            fs.RemoveAccessRule(New FileSystemAccessRule(Environment.UserName, FileSystemRights.FullControl, AccessControlType.Deny))
            File.SetAccessControl(Cartella, fs)
        Catch ex As Exception

        End Try
    End Sub

    Private Declare Function WNetGetConnection Lib "mpr.dll" Alias _
             "WNetGetConnectionA" (ByVal lpszLocalName As String, _
             ByVal lpszRemoteName As String, ByRef cbRemoteName As Integer) As Integer

    Public Function PrendePercorsoDiReteDelDisco(Lettera As String) As String
        Dim ret As Integer
        Dim out As String = New String(" ", 260)
        Dim len As Integer = 260

        ret = WNetGetConnection(Lettera, out, len)

        Return out.Replace(Chr(0), "").Trim
    End Function
End Class
