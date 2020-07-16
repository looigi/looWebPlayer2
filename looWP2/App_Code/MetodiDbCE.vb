Public Class MetodiDbCE
    Private varConnessione As SQLSERVERCE
    Private conn As Object = CreateObject("ADODB.Connection")

    Public Function ApreConnessione(PathDB As String, NomeDb As String) As String
        varConnessione = New SQLSERVERCE
        varConnessione.ImpostaNomeDB(PathDB & "\" & NomeDb)
        varConnessione.LeggeImpostazioniDiBase()
        conn = varConnessione.ApreDB()

        If TypeOf (conn) Is String Then
            Return conn
        Else
            Return "OK"
        End If
    End Function

    Public Function RitornaRecordset(Query As String) As Object
        Dim rec As Object = "ADODB.Recordset"

        Try
            rec = varConnessione.LeggeQuery(conn, Query)
            'If rec.Eof Then
            '    rec.Close()
            '    rec = Nothing
            'End If
        Catch ex As Exception
            rec = Nothing
        End Try

        Return rec
    End Function

    Public Function EsegueSQL(Query As String) As String
        Dim ok As String = ""

        Try
            varConnessione.EsegueSql(conn, Query)
            ok = "OK"
        Catch ex As Exception
            ok = ex.Message
        End Try

        Return ok
    End Function

    Public Sub ChiudeConnessione()
        If Not conn Is Nothing Then
            conn.Close()
        End If
        varConnessione = Nothing
    End Sub
End Class
