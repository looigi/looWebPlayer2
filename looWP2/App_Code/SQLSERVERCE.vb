Public Class SQLSERVERCE
    Private Connessione As String
    Private NomeDB As String

    Public Function ProvaConnessione() As String
        Dim Conn As Object = CreateObject("ADODB.Connection")

        Try
            Conn.Open(Connessione)
            Conn.Close()

            Conn = Nothing
            Return ""
        Catch ex As Exception

            Return ex.Message
        End Try
    End Function

    Public Sub ImpostaNomeDB(Nome)
        NomeDB = Nome ' Application.StartupPath & "\DB\dbBackup.sdf;"
    End Sub

    Public Function LeggeImpostazioniDiBase() As Boolean
        Dim connectionString As String = "Data Source=" & NomeDB

        Connessione = "Provider=" & "Microsoft.SQLSERVER.CE.OLEDB.4.0;" & ";" & connectionString

        Return True
    End Function

    Public Function ApreDB() As Object
        ' Routine che apre il DB e vede se ci sono errori
        Dim Conn As Object = CreateObject("ADODB.Connection")

        Try
            Conn.Open(Connessione)
            Conn.CommandTimeout = 0
        Catch ex As Exception
            ' MsgBox("APERTURA DB: " & ex.Message)
            Return "ERROR: APERTURA DB: " & ex.Message
        End Try

        Return Conn
    End Function

    Private Function ControllaAperturaConnessione(ByRef Conn As Object) As Boolean
        Dim Ritorno As Boolean = False

        If Conn Is Nothing And Not TypeOf (Conn) Is String Then
            Ritorno = True
            Conn = ApreDB()
        End If

        Return Ritorno
    End Function

    Public Function EsegueSql(ByVal Conn As Object, ByVal Sql As String) As String
        Dim AperturaManuale As Boolean = ControllaAperturaConnessione(Conn)
        Dim Errore As String = ""

        ' Routine che esegue una query sul db
        Try
            Conn.Execute(Sql)
        Catch ex As Exception
            Errore = ex.Message
        End Try

        ChiudeDB(AperturaManuale, Conn)

        Return Errore
    End Function

    Private Sub ChiudeDB(ByVal TipoApertura As Boolean, ByRef Conn As Object)
        If TipoApertura = True Then
            Conn.Close()
        End If
    End Sub

    Public Function LeggeQuery(ByVal Conn As Object, ByVal Sql As String) As Object
        Dim AperturaManuale As Boolean = ControllaAperturaConnessione(Conn)
        Dim Rec As Object = CreateObject("ADODB.Recordset")

        Try
            Rec.Open(Sql, Conn)
        Catch ex As Exception
            Rec = Nothing
        End Try

        ChiudeDB(AperturaManuale, Conn)

        Return Rec
    End Function

End Class
