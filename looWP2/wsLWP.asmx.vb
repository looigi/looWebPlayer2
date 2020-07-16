Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.ComponentModel
Imports System.IO
Imports System.Web.Script.Services
Imports System.Threading

' Per consentire la chiamata di questo servizio Web dallo script utilizzando ASP.NET AJAX, rimuovere il commento dalla riga seguente.
' <System.Web.Script.Services.ScriptService()> _
<System.Web.Services.WebService(Namespace:="http://wsLWP2.org/")>
<System.Web.Services.WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)>
<ScriptService>
<ToolboxItem(False)>
Public Class wsLWP
	Inherits System.Web.Services.WebService

	<WebMethod()>
	Public Function RefreshCanzoni() As String
		Dim pathMP3 As String = RitornaPercorso(1) & "\"
		Dim mDBCE As New MetodiDbCE
		Dim gf As New GestioneFilesDirectory
		Dim NomeDB As String = Server.MapPath(".") & "\DB\MP3.sdf"
		Dim Ritorno As String = ""

		Ritorno = mDBCE.ApreConnessione(gf.TornaNomeDirectoryDaPath(NomeDB), gf.TornaNomeFileDaPath(NomeDB))
		If Ritorno <> "OK" Then
			Return Ritorno
		End If

		gf.ScansionaDirectorySingola(pathMP3)
		Dim filetti() As String = gf.RitornaFilesRilevati()
		Dim qFiletti As Integer = gf.RitornaQuantiFilesRilevati()

		Dim sql As String = ""

		sql = "Delete From Artisti"
		Ritorno = mDBCE.EsegueSQL(sql)
		If Ritorno = "OK" Then
			sql = "Delete From ListaCanzoni"
			Ritorno = mDBCE.EsegueSQL(sql)
			If Ritorno = "OK" Then
				sql = "Delete From Immagini"
				Ritorno = mDBCE.EsegueSQL(sql)
				If Ritorno = "OK" Then
					Dim idArtista As Integer = 0
					Dim idImmagine As Integer = 0
					Dim vecchioArtista As String = ""
					Dim idCanzone As Integer = 0

					For i As Integer = 1 To qFiletti
						Dim canzone As String = filetti(i).Replace(pathMP3, "")
						Dim campi() As String = canzone.Split("\")
						Dim Artista As String = campi(0)
						Dim Album As String = campi(1)
						Dim Brano As String = campi(2)

						If vecchioArtista <> Artista Then
							vecchioArtista = Artista
							idArtista += 1
							sql = "Insert Into Artisti Values (" & idArtista & ", '" & Artista.Replace("'", "''") & "')"
							Ritorno = mDBCE.EsegueSQL(sql)
							If Ritorno <> "OK" Then
								Ritorno = "ERROR: " & Ritorno
								Exit For
							End If
						End If

						If filetti(i).ToUpper.Contains(".MP3") Or filetti(i).ToUpper.Contains(".WAV") Or filetti(i).ToUpper.Contains(".WMA") Then
							idCanzone += 1
							sql = "Insert Into ListaCanzoni Values (" &
								" " & idCanzone & ", " &
								" " & idArtista & ", " &
								"'" & Album.Replace("'", "''") & "', " &
								"'" & Brano.Replace("'", "''") & "' " &
								")"
						Else
							idImmagine += 1
							sql = "Insert Into Immagini Values (" &
								" " & idArtista & ", " &
								" " & idImmagine & ", " &
								"'" & Album.Replace("'", "''") & "', " &
								"'" & Brano.Replace("'", "''") & "' " &
								")"
						End If
						Ritorno = mDBCE.EsegueSQL(sql)
						If Ritorno <> "OK" Then
							Ritorno = "ERROR: " & Ritorno
							Exit For
						End If
					Next
				Else
					Ritorno = "ERROR: " & Ritorno
				End If
			Else
				Ritorno = "ERROR: " & Ritorno
			End If
		Else
			Ritorno = "ERROR: " & Ritorno
		End If

		mDBCE.ChiudeConnessione()

		Dim Ritorno2 As String = CreaJSONBrani()

		gf = Nothing
		Return "*"
	End Function

	<WebMethod()>
	Public Function RitornaProssimoBrano(Random As String, vecchioBrano As String) As String
		Dim pathMP3 As String = RitornaPercorso(1) & "\"
		Dim pathMP3Tag As String = RitornaPercorso(2) & "\"
		Dim mDBCE1 As New MetodiDbCE
		Dim mDBCE2 As New MetodiDbCE
		Dim gf As New GestioneFilesDirectory
		Dim NomeDB As String = Server.MapPath(".") & "\DB\MP3.sdf"
		Dim Ritorno As String = ""

		Ritorno = mDBCE1.ApreConnessione(gf.TornaNomeDirectoryDaPath(NomeDB), gf.TornaNomeFileDaPath(NomeDB))
		If Ritorno <> "OK" Then
			Return Ritorno
		End If

		Ritorno = mDBCE2.ApreConnessione(gf.TornaNomeDirectoryDaPath(pathMP3Tag), gf.TornaNomeFileDaPath(pathMP3Tag))
		If Ritorno <> "OK" Then
			Return Ritorno
		End If

		Dim Sql As String = "Select Max(id) From ListaCanzoni"
		Dim rec As Object = mDBCE1.RitornaRecordset(Sql)
		If rec Is Nothing Then
			Ritorno = "ERROR: query non valida -> " & Sql
		Else
			Dim quantiBrani As Integer = -1

			If Not rec(0).Value Is DBNull.Value Then
				quantiBrani = rec(0).Value
				rec.close()

				Dim prossimo As Integer = -1

				If Random = "S" Then
					Static numeroRandom As New Random()
					prossimo = numeroRandom.Next(1, quantiBrani)
				Else
					prossimo = Val(vecchioBrano) + 1
					If prossimo > quantiBrani Then
						prossimo = 1
					End If
				End If

				Dim Artista As String =
				Sql = "Select B.Artista From ListaCanzoni A Left Join Artisti B On A.idArtista=B.idArtista Where id = " & prossimo
				rec = mDBCE1.RitornaRecordset(Sql)
				If Not rec.eof() Then
					Artista = rec("Artista").value
				End If
				rec.close()

				Dim gifs As String = RitornaGifScaricate(Artista)

				ScaricaGIF(Artista)

				Ritorno = PrendeBrano(prossimo, quantiBrani, rec, mDBCE1, mDBCE2, gifs)
			Else
				Ritorno = "ERROR: nessun brano in archivio"
			End If
		End If
		mDBCE1.ChiudeConnessione()

		Return Ritorno
	End Function

	Public Function RitornaGifScaricate(artista)
		Dim ritorno As String = ""

		If Directory.Exists(Server.MapPath(".") & "\Gifs\" & artista) Then
			Dim strFileSize As String = ""
			Dim di As New IO.DirectoryInfo(Server.MapPath(".") & "\Gifs\" & artista)
			Dim aryFi As IO.FileInfo() = di.GetFiles("*.gif")
			Dim fi As IO.FileInfo

			For Each fi In aryFi
				ritorno &= "0;" & artista & ";" & fi.Name & ";§"
			Next
		End If

		Return ritorno
	End Function

	<WebMethod()>
	Public Function RitornaBranoDaID(Brano As String) As String
		Dim pathMP3 As String = RitornaPercorso(1) & "\"
		Dim pathMP3Tag As String = RitornaPercorso(2)
		Dim mDBCE1 As New MetodiDbCE
		Dim mDBCE2 As New MetodiDbCE
		Dim gf As New GestioneFilesDirectory
		Dim NomeDB As String = Server.MapPath(".") & "\DB\MP3.sdf"
		Dim Ritorno As String = ""

		Ritorno = mDBCE1.ApreConnessione(gf.TornaNomeDirectoryDaPath(NomeDB), gf.TornaNomeFileDaPath(NomeDB))
		If Ritorno <> "OK" Then
			Return Ritorno
		End If

		Ritorno = mDBCE2.ApreConnessione(gf.TornaNomeDirectoryDaPath(pathMP3Tag), gf.TornaNomeFileDaPath(pathMP3Tag))
		If Ritorno <> "OK" Then
			Return Ritorno
		End If

		Dim Sql As String = "Select Max(id) From ListaCanzoni"
		Dim rec As Object = mDBCE1.RitornaRecordset(Sql)
		If rec Is Nothing Then
			Ritorno = "ERROR: query non valida -> " & Sql
		Else
			Dim quantiBrani As Integer = -1

			If Not rec(0).Value Is DBNull.Value Then
				quantiBrani = rec(0).Value
				rec.close()

				Dim Artista As String = ""
				Sql = "Select B.Artista From ListaCanzoni A Left Join Artisti B On A.idArtista=B.idArtista Where id = " & Brano
				rec = mDBCE1.RitornaRecordset(Sql)
				If Not rec.eof() Then
					Artista = rec("Artista").value
				End If
				rec.close()

				Dim gifs As String = RitornaGifScaricate(Artista)

				ScaricaGIF(Artista)

				Ritorno = PrendeBrano(Brano, quantiBrani, rec, mDBCE1, mDBCE2, gifs)
			Else
				Ritorno = "ERROR: nessun brano in archivio"
			End If
		End If
		mDBCE1.ChiudeConnessione()

		Return Ritorno
	End Function

	Public Function CreaJSONBrani() As String
		Dim pathMP3 As String = RitornaPercorso(1) & "\"
		Dim mDBCE As New MetodiDbCE
		Dim NomeDB As String = Server.MapPath(".") & "\DB\MP3.sdf"
		Dim Ritorno As String = ""
		Dim gf As New GestioneFilesDirectory

		Ritorno = mDBCE.ApreConnessione(gf.TornaNomeDirectoryDaPath(NomeDB), gf.TornaNomeFileDaPath(NomeDB))
		If Ritorno <> "OK" Then
			Return Ritorno
		End If

		Dim Sql As String = "Select * From Artisti Order By Artista"
		Dim recArtista As Object = mDBCE.RitornaRecordset(Sql)
		Dim contatoreArtista As Integer = 0
		Dim contatoreAlbum As Integer = 0
		Dim contatoreBrano As Integer = 0
		Dim cont As String = ""

		If recArtista Is Nothing Then
			Ritorno = "ERROR: query non valida -> " & Sql
		Else
			Ritorno = "["
			Do Until recArtista.Eof()
				contatoreArtista += 1
				contatoreAlbum = 0
				contatoreBrano = 0
				cont = contatoreArtista & contatoreAlbum & contatoreBrano
				Ritorno &= "{ ""text"": """ & recArtista("Artista").Value & """, ""value"": " & cont & ", ""collapsed"": true "

				Sql = "Select Distinct Album From ListaCanzoni Where idArtista = " & recArtista("idArtista").Value & " Order By Album"
				Dim recAlbum As Object = mDBCE.RitornaRecordset(Sql)
				If recAlbum Is Nothing Then
					Ritorno = "ERROR: query non valida -> " & Sql
				Else
					If Not recAlbum.Eof() Then
						Ritorno &= ", ""children"": ["
						contatoreAlbum = 0
						Do Until recAlbum.eof()
							contatoreAlbum += 1
							contatoreBrano = 0
							cont = contatoreArtista & contatoreAlbum & contatoreBrano
							Ritorno &= "{ ""text"": """ & recAlbum("Album").value & """, ""value"": " & cont & ", ""collapsed"": true "

							Sql = "Select * From ListaCanzoni Where idArtista = " & recArtista("idArtista").Value & " And Album = '" & recAlbum("Album").value.replace("'", "''") & "' Order By Brano"
							Dim recBrano As Object = mDBCE.RitornaRecordset(Sql)
							If recBrano Is Nothing Then
								Ritorno = "ERROR: query non valida -> " & Sql
							Else
								If Not recBrano.Eof() Then
									Ritorno &= ", ""children"": ["
									contatoreBrano = 0
									Do Until recBrano.eof()
										contatoreBrano += 1
										cont = contatoreArtista & contatoreAlbum & contatoreBrano
										Ritorno &= "{ ""text"": """ & recBrano("Brano").value & """, ""value"": " & cont & ", ""id"": " & recBrano("id").value & " },"

										recBrano.moveNext()
									Loop
									Ritorno = Mid(Ritorno, 1, Len(Ritorno) - 1)
									Ritorno &= "] },"
								Else
									Ritorno &= "},"
								End If
							End If

							recAlbum.moveNext()
						Loop
					Else
						Ritorno = Mid(Ritorno, 1, Len(Ritorno) - 1)
						Ritorno &= "] }] },"
					End If
				End If
				Ritorno = Mid(Ritorno, 1, Len(Ritorno) - 1)
				Ritorno &= "]},"

				recArtista.moveNext()
			Loop
			Ritorno = Mid(Ritorno, 1, Len(Ritorno) - 1)
			Ritorno &= "]"
			recArtista.Close()
		End If
		mDBCE.ChiudeConnessione()

		gf.EliminaFileFisico(Server.MapPath(".") & "\Canzoni.json")
		gf.ApreFileDiTestoPerScrittura(Server.MapPath(".") & "\Canzoni.json")
		gf.ScriveTestoSuFileAperto(Ritorno)
		gf.ChiudeFileDiTestoDopoScrittura()
		gf = Nothing

		Return Ritorno
	End Function

	<WebMethod()>
	Public Function RitornaJSON() As String
		Dim gf As New GestioneFilesDirectory
		Dim Ritorno As String = ""
		If File.Exists(Server.MapPath(".") & "\Canzoni.json") Then
			Ritorno = gf.LeggeFileIntero(Server.MapPath(".") & "\Canzoni.json")
		Else
			Ritorno = CreaJSONBrani()
		End If
		gf = Nothing

		Return Ritorno
	End Function

	<WebMethod()>
	Public Function CreaRitornaUtenza(Utenza As String) As String
		Dim Ritorno As String = ""
		Dim pathMP3 As String = RitornaPercorso(1) & "\"
		Dim mDBCE As New MetodiDbCE
		Dim gf As New GestioneFilesDirectory
		Dim NomeDB As String = Server.MapPath(".") & "\DB\MP3.sdf"
		Dim Sql As String
		Dim rec As Object
		Dim idUtente As String = -1

		Ritorno = mDBCE.ApreConnessione(gf.TornaNomeDirectoryDaPath(NomeDB), gf.TornaNomeFileDaPath(NomeDB))
		If Ritorno <> "OK" Then
			Return Ritorno
		End If

		If Utenza = "" Then
			Dim Utente As String = ""
			Dim a As String = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_=)(&%$£!"
			For i As Integer = 0 To 15
				Static numeroRandom As New Random()
				Dim prossimo As Integer = numeroRandom.Next(1, a.Length - 1)
				Utente &= Mid(a, prossimo, 1)
			Next

			Sql = "Select Max(idUtente) + 1 From Utenti"
			rec = mDBCE.RitornaRecordset(Sql)
			If rec Is Nothing Then
				Ritorno = "ERROR: query non valida -> " & Sql
			Else
				If rec(0).Value Is DBNull.Value Then
					idUtente = 1
				Else
					idUtente = rec(0).Value
				End If
				rec.Close()

				Sql = "Insert Into Utenti Values(" & idUtente & ", '" & Utente & "')"
				Ritorno = mDBCE.EsegueSQL(Sql)
				If Ritorno = "OK" Then
					Ritorno = idUtente & ";" & Utente
				End If
			End If
		Else
			Sql = "Select idUtente From Utenti Where Utente='" & Utenza & "'"
			rec = mDBCE.RitornaRecordset(Sql)
			If rec Is Nothing Then
				Ritorno = "ERROR: query non valida -> " & Sql
			Else
				If rec.eof() Then
					Ritorno = "ERROR: Nessun utente rilevato."
				Else
					idUtente = rec(0).Value
					Ritorno = idUtente & ";" & Utenza
				End If
				rec.Close()
			End If
		End If

		Return Ritorno
	End Function

	<WebMethod()>
	Public Function ScaricaGIF(Artista As String) As String
		Dim thread As New Thread(Sub() Me.ScaricoGIF(Artista))
		thread.Start()

		Return "*"
	End Function

	Private Sub ScaricoGIF(Artista As String)
		Dim nomeFilePrese As String = Server.MapPath(".") & "\GIFS\" & Artista & "\GiaPrese.txt"
		If File.Exists(nomeFilePrese) Then
			Dim dataModifica As Date = FileDateTime(nomeFilePrese)
			Dim diff As TimeSpan = Now.Subtract(dataModifica)
			Dim giorni As Integer = diff.Days
			If giorni < 7 Then
				Exit Sub
			End If
		End If

		Dim dl As New download
		Dim gf As New GestioneFilesDirectory
		gf.CreaDirectoryDaPercorso(Server.MapPath(".") & "\Gifs\" & Artista & "\")
		Dim nomeFile As String = Server.MapPath(".") & "\Gifs\" & Artista & "\Scarico.txt"
		Dim nomeFileQuante As String = Server.MapPath(".") & "\Gifs\" & Artista & "\Quante.txt"

		dl.ScaricaFile("https://it.images.search.yahoo.com/search/images;_ylt=AwrJS9KY4Q5fiBMAa0cbDQx.;_ylu=X3oDMTB0ZTgxN3Q0BGNvbG8DaXIyBHBvcwMxBHZ0aWQDBHNlYwNwaXZz?p=" & Artista & "+gif&fr2=piv-web&fr=yfp-t", nomeFile)
		Dim tutto As String = gf.LeggeFileIntero(nomeFile)
		Dim giaPrese As String = gf.LeggeFileIntero(nomeFilePrese)
		Dim QuanteGifs As Integer = Val(gf.LeggeFileIntero(nomeFileQuante))
		Dim numeroGif As Integer = QuanteGifs + 1
		gf.ApreFileDiTestoPerScrittura(nomeFilePrese)

		Dim daCercare As String = Chr(34) & "iurl" & Chr(34) & ":" & Chr(34)
		While tutto.Contains(daCercare)
			Dim parte1 As String = Mid(tutto, tutto.IndexOf(daCercare) + Len(daCercare) + 1, tutto.Length)
			Dim fine As Integer
			fine = parte1.IndexOf(Chr(34))
			Dim parte2 As String = Mid(parte1, 1, fine)
			Dim origParte As String = parte2
			parte2 = parte2.Replace("\/", "/")

			If parte2 <> "" Then
				If Not giaPrese.Contains(parte2) Then
					giaPrese &= parte2 & "§"
					Dim rit As Boolean = dl.ScaricaFile(parte2, Server.MapPath(".") & "\GIFS\" & Artista & "\" & Format(numeroGif, "000") & ".gif")
					If rit Then
						numeroGif += 1
					End If
				End If
				parte2 = daCercare & origParte
			Else
				Dim a As Integer = tutto.IndexOf(daCercare)
				parte2 = Mid(tutto, a, Len(daCercare) + 3)
			End If

			tutto = tutto.Replace(parte2, "")
		End While
		gf.ScriveTestoSuFileAperto(giaPrese)
		gf.ChiudeFileDiTestoDopoScrittura()

		gf.EliminaFileFisico(nomeFileQuante)
		gf.ApreFileDiTestoPerScrittura(nomeFileQuante)
		gf.ScriveTestoSuFileAperto(numeroGif)
		gf.ChiudeFileDiTestoDopoScrittura()

		gf.EliminaFileFisico(nomeFile)
	End Sub
End Class