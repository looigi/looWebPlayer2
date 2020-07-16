Module mdlGenerale

	Public Function RitornaPercorso(Quale As Integer) As String
		Dim gf As New GestioneFilesDirectory
		Dim tutto As String = gf.LeggeFileIntero(HttpContext.Current.Server.MapPath(".") & "\PathMP3.txt")
		Dim righe() As String = tutto.Split(";")
		Dim ritorno As String = righe(Quale - 1)
		ritorno = Mid(ritorno, ritorno.IndexOf("=") + 2, ritorno.Length)
		gf = Nothing
		Return ritorno
	End Function

	Public Function PrendeBrano(Prossimo As String, quantiBrani As Integer, rec As Object, mDBCE1 As MetodiDbCE, mDBCE2 As MetodiDbCE, Gifs As String) As String
		Dim Sql As String
		Dim Ritorno As String = ""

		Sql = "Select B.Artista, A.Album, A.Brano, A.idArtista From ListaCanzoni A Left Join Artisti B On A.idArtista = B.idArtista Where A.id = " & Prossimo
		rec = mDBCE1.RitornaRecordset(Sql)
		If rec Is Nothing Then
			Ritorno = "ERROR: query non valida -> " & Sql
		Else
			If Not rec.eof() Then
				Dim idArtista As Integer = rec(3).Value
				Dim Artista As String = rec(0).value
				Dim Album As String = rec(1).value
				Dim Brano As String = rec(2).value

				Ritorno = Prossimo & ";" & quantiBrani & ";" & idArtista & ";" & Artista.Replace(";", "*") & ";" & Album.Replace(";", "*") & ";" & Brano.Replace(";", "*") & ";|"
				rec.close()

				Sql = "Select * From Immagini Where idArtista = " & idArtista
				rec = mDBCE1.RitornaRecordset(Sql)
				If rec Is Nothing Then
					Ritorno = "ERROR: query non valida -> " & Sql
				Else
					Do Until rec.eof()
						Ritorno &= rec("idImmagine").value & ";" & rec("Cartella").value.replace(";", "*") & ";" & rec("Immagine").value.replace(";", "*") & ";§"

						rec.moveNext()
					Loop
					rec.close()

					Ritorno &= Gifs

					' Prende dati da MP3Tag
					Ritorno &= "|"

					Dim Anno As String = "0"
					Dim Traccia As String = "0"
					Dim Estensione As String = ""

					If Album.Contains("-") Then
						Dim c() As String = Album.Split("-")
						Anno = c(0)
						Album = c(1)
					End If
					If Brano.Contains("-") Then
						Dim c() As String = Brano.Split("-")
						Traccia = c(0)
						Brano = c(1)
					End If
					If Brano.Contains(".") Then
						Dim c() As String = Brano.Split(".")
						Estensione = c(1).ToUpper.Trim
						Brano = c(0)
					End If

					Sql = "Select * From ListaCanzone2 Where Artista = '" & Artista.Replace("'", "''") & "' And Album = '" & Album.Replace("'", "''") & "' And Canzone='" & Brano.Replace("'", "''") & "' And Traccia='" & Traccia & "' And Anno=" & Anno & " And Estensione='" & Estensione & "'"
					rec = mDBCE2.RitornaRecordset(Sql)
					If Not rec.Eof() Then
						Ritorno &= rec("Ascoltata").value & ";" & rec("Bellezza").value & ";" & rec("Testo").value.replace(";", "***PV***") & ";" & rec("TestoTradotto").value.replace(";", "***PV***") & ";"
					Else
						Ritorno &= ";;;;"
					End If
					rec.close()
				End If

			Else
				Ritorno = "ERROR: nessun brano rilevato. ID: " & Prossimo
			End If
		End If

		Return Ritorno
	End Function

End Module
