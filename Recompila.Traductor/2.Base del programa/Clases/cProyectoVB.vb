﻿Imports Recompila.Helper

Public Class cProyectoVB
#Region " PROPIEDADES "
    ''' <summary>
    ''' Ruta del proyecto .vbproject que está cargado
    ''' </summary>
    Public ReadOnly Property rutaProyecto As String
        Get
            Return iRutaProyecto
        End Get
    End Property
    Private iRutaProyecto As String = ""

    ''' <summary>
    ''' Ruta de la carpeta del proyecto
    ''' </summary>
    Public ReadOnly Property carpetaProyecto As String
        Get
            If IO.File.Exists(iRutaProyecto) Then
                Return Ficheros.extraerRutaFichero(iRutaProyecto)
            Else
                Return ""
            End If
        End Get
    End Property

    ''' <summary>
    ''' Ruta de la carpeta donde se encuentran las traducciones
    ''' </summary>
    Public ReadOnly Property carpetaTraducciones As String
        Get
            Return carpetaProyecto & "\Languages\" & Criptografia.encriptarEnMD5(Ensamblado)
        End Get
    End Property

    ''' <summary>
    ''' Numero de la versión de traducción que se tiene que generar respetando las anteriores creadas
    ''' </summary>
    Public ReadOnly Property versionTraduccion As Integer
        Get
            Dim paraDevolver As Integer = 1

            If IO.Directory.Exists(carpetaTraducciones) Then
                Dim listaVersiones As New List(Of Long)
                Dim versionMaxima As Long = 0

                For Each unFichero As String In My.Computer.FileSystem.GetFiles(carpetaTraducciones)
                    Dim nombreFichero As String = Ficheros.extraerNombreFicheroSinExtension(unFichero)
                    If nombreFichero.Trim <> "" AndAlso nombreFichero.Contains("_") Then
                        Dim laVersion As Long = nombreFichero.Split(".")(0).Split("_")(1)
                        If versionMaxima < laVersion Then versionMaxima = laVersion
                        If Not listaVersiones.Contains(laVersion) Then listaVersiones.Add(laVersion)
                    End If
                Next

                paraDevolver = (versionMaxima + 1)
            Else
                System.IO.Directory.CreateDirectory(carpetaTraducciones)
            End If

            Return paraDevolver
        End Get
    End Property

    ''' <summary>
    ''' Obtiene un DataSet a partir de la información XML almacenada en el archivo .vbproj 
    ''' del proyecto con el que se está trabajando
    ''' </summary>
    Public ReadOnly Property DataSetXML As DataSet
        Get
            If IO.File.Exists(iRutaProyecto) Then
                Try
                    Dim elDataSet As New DataSet
                    elDataSet.ReadXml(iRutaProyecto)
                    Return elDataSet
                Catch ex As Exception
                    If Log._LOG_ACTIVO Then Log.escribirLog("ERROR al cargar el DataSetXML del proyecto de '" & iRutaProyecto & "'...", ex, New StackTrace(0, True))
                    Return Nothing
                End Try
            Else
                If Log._LOG_ACTIVO Then Log.escribirLog("ERROR al crear el DataSetXML del proyecto, no existe la ruta '" & iRutaProyecto & "'...", , New StackTrace(0, True))
                Return Nothing
            End If
        End Get
    End Property

    ''' <summary>
    ''' Nombre del ensamblado del proyecto
    ''' </summary>
    Public ReadOnly Property Ensamblado As String
        Get
            ' Si no existe la ruta del proyecto no se puede realiar la operación
            ' por lo que se devuelve una cadena vacía, lo que producirá un error
            ' al tratar de pasar a los siguientes pasos del asistente al no completar
            ' los campos
            If Not IO.File.Exists(iRutaProyecto) Then Return ""

            Dim paraDevolver As String = ""
            Try
                Dim elDataSet As DataSet = DataSetXML
                If elDataSet IsNot Nothing Then
                    Dim laTabla As DataTable = obtenerTabla(elDataSet, "PropertyGroup")
                    If laTabla IsNot Nothing Then
                        Dim laColumna As DataColumn = obtenerColumna(laTabla, "AssemblyName")
                        If laColumna IsNot Nothing Then
                            For Each unRow As DataRow In laTabla.Rows
                                paraDevolver = unRow.Item(laColumna).ToString.Trim
                                Exit For
                            Next
                        End If
                    End If
                End If
            Catch ex As Exception
                If Log._LOG_ACTIVO Then Log.escribirLog("ERROR al tratar de obtener el nombre del ensamblado...", ex, New StackTrace(0, True))
                paraDevolver = ""
            End Try

            Return paraDevolver
        End Get
    End Property
#End Region


#Region " CONSTRUCTORES "
    Public Sub New(ByVal eRutaProyecto As String)
        iRutaProyecto = eRutaProyecto
    End Sub
#End Region

#Region " METODOS PRIVADOS "
    ''' <summary>
    ''' Obtiene la tabla solicitada por parámetro al DataSeet que se le pasa como parámetro
    ''' </summary>
    ''' <param name="eDataSet">DataSet del que se tiene que extraer la tabla</param>
    ''' <param name="eNombreTabla">Nombre de la tabla que se quiere obtener</param>
    ''' <returns>La tabla o Nothing si no se encuentra</returns>
    Private Function obtenerTabla(ByVal eDataSet As DataSet, _
                                  ByVal eNombreTabla As String) As DataTable
        If eDataSet IsNot Nothing Then
            Dim laTabla As DataTable = (From it As DataTable In eDataSet.Tables _
                                        Where it.TableName.Trim.ToUpper = eNombreTabla.ToUpper.Trim _
                                        Select it).FirstOrDefault
            Return laTabla
        Else
            Return Nothing
        End If
    End Function

    ''' <summary>
    ''' Obtiene la columna solicitada por parámetro al DataTable que se le pasa como parámetro
    ''' </summary>
    ''' <param name="eDataTable">DataTable del que se tiene que extraer la columna</param>
    ''' <param name="eNombreColumna">Nombre de la columna que se quiere obtener</param>
    ''' <returns>La columna o Nothing si no se encuentra</returns>
    Private Function obtenerColumna(ByVal eDataTable As DataTable, _
                                    ByVal eNombreColumna As String) As DataColumn
        If eDataTable IsNot Nothing Then
            Dim laColumna As DataColumn = (From it As DataColumn In eDataTable.Columns _
                                           Where it.ColumnName.Trim.ToUpper = eNombreColumna.Trim.ToUpper _
                                           Select it).FirstOrDefault
            Return laColumna
        Else
            Return Nothing
        End If
    End Function
#End Region


End Class