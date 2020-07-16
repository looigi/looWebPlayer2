Imports System.IO
Imports System.Net

Public Class download
	Private TipoCollegamento As String
	Private Utenza As String
	Private Password As String
	Private Dominio As String
	Private sNomeFileConPercorso As String

	Public Function ScaricaFile(Url As String, Optional sNomeDestinazione As String = "") As Boolean
		Dim Ok As Boolean = True
		' Dim sourceCode As String
		If sNomeDestinazione = "" Then sNomeDestinazione = sNomeFileConPercorso
		Dim gf As New GestioneFilesDirectory

		If TipoCollegamento Is Nothing = True Then TipoCollegamento = ""

		If TipoCollegamento.Trim.ToUpper = "PROXY" Then
			Dim request As System.Net.HttpWebRequest = System.Net.HttpWebRequest.Create(Url)
			request.Proxy.Credentials = New System.Net.NetworkCredential(Utenza, Password, Dominio)
			Dim response As System.Net.HttpWebResponse = request.GetResponse()
			'Application.DoEvents()

			Dim responseStream As Stream = response.GetResponseStream()
			Dim imageBytes() As Byte

			Using br As New BinaryReader(responseStream)
				imageBytes = br.ReadBytes(500000)
				br.Close()
			End Using
			responseStream.Close()
			response.Close()

			Dim fs As New FileStream(sNomeFileConPercorso, FileMode.Create)
			Dim bw As New BinaryWriter(fs)
			Try
				bw.Write(imageBytes)
			Finally
				fs.Close()
				bw.Close()
			End Try

			request = Nothing

			response.Close()
			response = Nothing
			request = Nothing
		Else
			Dim myWebClient As New WebClient()

			Try
				myWebClient.DownloadFile(Url, sNomeDestinazione)
			Catch ex As Exception
				Return False
			End Try
		End If

		Return Ok
	End Function

End Class
