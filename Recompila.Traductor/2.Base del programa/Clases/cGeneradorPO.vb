﻿Imports System.IO
Imports Recompila.Helper
Imports System.Net
Imports System.Windows.Forms

''' <summary>
''' Clase encargada de la generación de los archivos PO con las traducciones
''' </summary>
Public Class cGeneradorPO

#Region " ENUMERADOS "
    ''' <summary>
    ''' La información de progreso se puede reaizar sobre un proceso de traducción
    ''' en concreto (barra secundaria), o sobre un proceso general (total de objetos
    ''' a traducir). Este enumerado lo utilizan los eventos de información de proceso
    ''' para indicar sobre que barra se está actuando
    ''' </summary>
    Public Enum TipoBarraProgreso
        Primaria = 1
        Secundaria = 2
    End Enum
#End Region

#Region " DECLARACIONES "
    ''' <summary>
    ''' Gestor de subidas al FTP. Este objeto se encargará de subir los archivos necesarios
    ''' para realizar la traducción al servidor FTP para poder acceder mediante HTTP para 
    ''' la realización de la traducción
    ''' </summary>
    Private WithEvents fileUploader As System.Net.WebClient = Nothing

    ''' <summary>
    ''' Flag que controla si se están subiendo ficheros al FTP
    ''' </summary>
    Private iSubiendo As Boolean = False

    ''' <summary>
    ''' Determina si el objeto generador debe lanzar eventos para informar del progreso 
    ''' </summary>
    Private iConEventos As Boolean = True

    ''' <summary>
    ''' Tag que se añade al final del HTML que se va a traducir para detectar el final
    ''' de la traducción o el fichero ya que algunos traductores OnLine no muestran la 
    ''' página completa con la traducción sino un 'Loading'.
    ''' </summary>
    Private Const _END_OF_FILE As String = "<b>__999__</b>"
#End Region

#Region " PROPIEDADES "
    ''' <summary>
    ''' Proyecto .NET con el que se va a trabajar
    ''' </summary>
    Private ReadOnly Property ProyectoNET As NET.cProyectoNET
        Get
            Return iProyectoVB
        End Get
    End Property
    Private iProyectoVB As NET.cProyectoVB = Nothing

    ''' <summary>
    ''' Proyecto Traductor con toda la configuración necesaria para realizar la traducción
    ''' </summary>
    Private ReadOnly Property ProyectoTraductor As cProyectoTraductor
        Get
            Return iProyectoTraductor
        End Get
    End Property
    Private iProyectoTraductor As cProyectoTraductor = Nothing

    ''' <summary>
    ''' Datos de acceso al servidor FTP/HTTP intermedio para las traducciones
    ''' </summary>            
    Private ReadOnly Property ConfiguracionNetwork As cConfiguracionNetwork
        Get
            Return iConfiguracionNetwork
        End Get
    End Property
    Private iConfiguracionNetwork As cConfiguracionNetwork = Nothing

    ''' <summary>
    ''' Motor de traducción que se va a utilizar para realizar las traducciones
    ''' </summary>
    Private ReadOnly Property Motor As Motor.IMotorTraduccion
        Get
            Return iMotor
        End Get
    End Property
    Private iMotor As Motor.IMotorTraduccion = Nothing

    ''' <summary>
    ''' Versión de la traducción que se va a realizar
    ''' </summary>
    Private ReadOnly Property VersionTraduccion As Integer
        Get
            Return iVersionTraduccion
        End Get
    End Property
    Private iVersionTraduccion As Integer = 1

    ''' <summary>
    ''' Idioma original del formulario desde el que se van a realizar las traducciones
    ''' </summary>
    Public IdiomaUso As cIdioma = Nothing

    ''' <summary>
    ''' Idiomas con los que va a atrabajar el objeto
    ''' </summary>
    Public ReadOnly Property Idiomas As List(Of cIdioma)
        Get
            Return iIdiomas
        End Get
    End Property
    Private iIdiomas As New List(Of cIdioma)
#End Region

#Region " EVENTOS "
    ''' <summary>
    ''' Evento que se lanza cada vez que se producen cambios para informar al usuario de estos
    ''' El manejador que está comprobando este evento se encargará de mostrar estos mensajes
    ''' mediante un cuadro de texto, un log, etc.
    ''' </summary>
    ''' <param name="eMensaje">Mensaje que se envía desde el traductor</param>
    Public Event notificarMensaje(ByVal eMensaje As String)

    ''' <summary>
    ''' Evento que se lanza para indicar el máximo valor que puede tomar la barra de progreso
    ''' </summary>
    ''' <param name="eBarra">Sobre que barra se va a actuar</param>
    ''' <param name="eValor">Valor máximo</param>
    Public Event notificarMaximo(ByVal eBarra As TipoBarraProgreso, ByVal eValor As Long)

    ''' <summary>
    ''' Evento que se lanza cada vez que se cambia el progreso de las tareas
    ''' </summary>
    ''' <param name="eBarra">Sobre que barra se va a actuar</param>
    Public Event notificarProgreso(ByVal eBarra As TipoBarraProgreso, _
                                   ByVal eValor As Integer)

    ''' <summary>
    ''' Evento que se lanza cada vez que finaliza la traducción de un formulario
    ''' </summary>
    ''' <param name="eBarra">Sobre que barra se va a actuar</param>
    Public Event notificarFinalizacion(ByVal eBarra As TipoBarraProgreso)
#End Region

#Region " MANEJADORES "
    ''' <summary>
    ''' Captura los mensajes enviados por el motor de traducción y los envía
    ''' mediante el evento notificarMensaje para ser procesados
    ''' </summary>
    ''' <param name="eMensaje">Mensaje devuelto por el motor</param>
    Private Sub manejadorNotificacionesMotor(eMensaje As String)
        If iConEventos Then RaiseEvent notificarMensaje(eMensaje)
    End Sub
#End Region

#Region " CONSTRUCTORES "
    Public Sub New(ByVal eConfiguracionNetwork As cConfiguracionNetwork, _
                   ByVal eProyectoNET As NET.cProyectoNET, _
                   ByVal eProyectoTraductor As cProyectoTraductor, _
                   ByVal eMotor As Motor.cMotorBase, _
                   Optional ByVal eConEventos As Boolean = True)

        ' Se guardan los objetos con las configuraciones
        iProyectoVB = eProyectoNET
        iProyectoTraductor = eProyectoTraductor
        iConfiguracionNetwork = eConfiguracionNetwork
        iMotor = eMotor
        iConEventos = eConEventos

        ' Se añade el manejador para capturar los mensajes enviados por el
        ' motor y poder mostrarlos en el proceso
        If iMotor IsNot Nothing Then
            AddHandler iMotor.notificarMensaje, AddressOf manejadorNotificacionesMotor
        End If

        ' Se crea el idioma original de la aplicación
        IdiomaUso = New cIdioma(ProyectoTraductor.IdiomaOrigen)

        ' Se añaden todos los idiomas a los que se va a traducir la aplicación
        Dim seAnhadioIdiomaUso As Boolean = False
        For Each unIdioma As idiomaLocalizacion In ProyectoTraductor.IdiomasDestino
            If unIdioma = IdiomaUso.codigoLocalizacion Then seAnhadioIdiomaUso = True
            Dim elIdioma As New cIdioma(unIdioma)
            If Not Idiomas.Contains(elIdioma) Then Idiomas.Add(elIdioma)
        Next

        ' Si no está añadido el idioma del que se va a traducir a los idiomas
        ' a traducir este se añade, para que se pueda seleccionar en la configuración
        ' del programa
        If Not seAnhadioIdiomaUso Then Idiomas.Add(IdiomaUso)

        ' Se guarda la versión de traducción a generar
        iVersionTraduccion = ProyectoNET.versionTraduccion
    End Sub
#End Region

#Region " TRADUCTOR "
    ''' <summary>
    ''' Realiza la traducción a partir de los parámetros configurados
    ''' </summary>
    ''' <returns>True o Fals dependiendo del resultado de la traducción</returns>
    Public Function Traducir() As Boolean
        Dim paraDevolver As Boolean = True
        Dim elMensaje As String = String.Empty

        ' Se guarda el momento inicial para poder realizar cálculos de rendimiento
        Dim momentoInicio As DateTime = Now

        ' Se inicializa el traductor y se lanzan los eventos iniciales
        ' -----------------------------------------------------------------------------------
        If iConEventos Then RaiseEvent notificarMensaje("Iniciando proceso de traducción...")
        If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Primaria)
        If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Secundaria)

        ' Se crea la carpeta para los lenguajes si todavía no existe
        ' -----------------------------------------------------------------------------------
        If iConEventos Then RaiseEvent notificarMensaje("Creando estructura de carpetas de salida...")
        If iConEventos Then RaiseEvent notificarMensaje("+ " & ProyectoNET.carpetaLanguages)
        If Not IO.Directory.Exists(ProyectoNET.carpetaLanguages) Then
            Try
                IO.Directory.CreateDirectory(ProyectoNET.carpetaLanguages)
            Catch ex As Exception
                elMensaje = "! ERROR al crear la carpeta " & ProyectoNET.carpetaLanguages
                If iConEventos Then RaiseEvent notificarMensaje(elMensaje)
                Throw New Exception(elMensaje, ex)
            End Try
        End If

        If iConEventos Then RaiseEvent notificarMensaje("+ " & ProyectoNET.carpetaTraducciones)
        If Not IO.Directory.Exists(ProyectoNET.carpetaTraducciones) Then
            Try
                IO.Directory.CreateDirectory(ProyectoNET.carpetaTraducciones)
            Catch ex As Exception
                elMensaje = "! ERROR al crear la carpeta " & ProyectoNET.carpetaTraducciones
                If iConEventos Then RaiseEvent notificarMensaje(elMensaje)
                Throw New Exception(elMensaje, ex)
            End Try
        End If

        ' Se guarda la anterior versión de traducción
        ' -----------------------------------------------------------------------------------
        If iConEventos Then RaiseEvent notificarMensaje("Cambiando versión de traducción a " & VersionTraduccion & "...")
        ' Se copian los ficheros actuales en la versión de generación, los cuales serán
        ' utilizados para comprobar si el texto ya está traducido o no. 
        ' Al copiar los ficheros antiguos estos sirven como copia de seguridad de las traducciones anteriores
        ' evitando perder traducciones que se pudieran haber realizado a mano
        Dim rutaOrigen As String = ""
        Dim rutaDestino As String = ""

        Dim lasTraduccionesAntiguas = My.Computer.FileSystem.GetFiles(ProyectoNET.carpetaTraducciones, FileIO.SearchOption.SearchTopLevelOnly, "*.po")
        If lasTraduccionesAntiguas IsNot Nothing AndAlso lasTraduccionesAntiguas.Count > 0 Then
            If iConEventos Then RaiseEvent notificarMaximo(TipoBarraProgreso.Secundaria, lasTraduccionesAntiguas.Count + 1)
            For Each unArchivo As String In lasTraduccionesAntiguas
                Try
                    ' Solamente se van a copiar las traducciones que no contengan el caracter _, las cuales
                    ' se corresponden a la última traducción realizada o utilizada
                    If Not Ficheros.extraerNombreFichero(unArchivo).Contains("_") Then
                        rutaOrigen = unArchivo
                        rutaDestino = ProyectoNET.carpetaTraducciones & Ficheros.extraerNombreFicheroSinExtension(rutaOrigen).Split("_")(0) & "_" & VersionTraduccion & ".po"

                        elMensaje = ("> " & Ficheros.extraerNombreFichero(rutaOrigen) & " -> " & Ficheros.extraerNombreFichero(rutaDestino))
                        If iConEventos Then RaiseEvent notificarMensaje(elMensaje)

                        If IO.File.Exists(rutaDestino) Then IO.File.Delete(rutaDestino)
                        If IO.File.Exists(rutaOrigen) Then IO.File.Move(rutaOrigen, rutaDestino)
                    End If
                Catch ex As Exception
                    If iConEventos Then RaiseEvent notificarMensaje("! ERROR - Al guardar " & Ficheros.extraerNombreFichero(rutaDestino))
                End Try

                If iConEventos Then RaiseEvent notificarProgreso(TipoBarraProgreso.Secundaria, 0)
            Next
            If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Secundaria)
        End If

        ' Se crean todos los ficheros PO de salida con la cabecera de cada fichero
        ' -----------------------------------------------------------------------------------        
        If iConEventos Then RaiseEvent notificarMensaje("Creando cabeceras de ficheros PO de la versión " & VersionTraduccion & "...")

        If iConEventos Then RaiseEvent notificarMaximo(TipoBarraProgreso.Secundaria, iIdiomas.Count + 1)
        For Each UnIdioma As cIdioma In iIdiomas
            Try
                rutaDestino = ProyectoNET.carpetaTraducciones & UnIdioma.strCodigoLocalizacion & ".po"

                elMensaje = ("> Generando cabecera PO de " & Ficheros.extraerNombreFichero(rutaDestino))
                If iConEventos Then RaiseEvent notificarMensaje(elMensaje)
                If iConEventos Then RaiseEvent notificarProgreso(TipoBarraProgreso.Secundaria, 0)

                If IO.File.Exists(rutaDestino) Then IO.File.Delete(rutaDestino)
                Dim elEscritorPO As New StreamWriter(rutaDestino, False, System.Text.Encoding.UTF8)
                EscribirCabeceraPO(elEscritorPO, UnIdioma.strCodigoLocalizacion)
                elEscritorPO.Close()
            Catch ex As Exception
                If iConEventos Then RaiseEvent notificarMensaje("! ERROR - Al crear la cabecera PO de " & Ficheros.extraerNombreFichero(rutaDestino))
            End Try
        Next
        If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Secundaria)

        ' Se recorren todos los objetos a traducir seleccionados por el usuario
        ' -----------------------------------------------------------------------------------        
        If iConEventos Then RaiseEvent notificarMaximo(TipoBarraProgreso.Primaria, iProyectoTraductor.ArchivosNET.Count + 1)
        For Each unArchivo As NET.cFormulario In iProyectoTraductor.ArchivosNET
            elMensaje = ("* Traduciendo " & unArchivo.NombreFichero)
            If iConEventos Then RaiseEvent notificarMensaje(elMensaje)
            If iConEventos Then RaiseEvent notificarProgreso(TipoBarraProgreso.Primaria, 0)

            ArchivoNET2ArchivoPO(unArchivo)
        Next
        If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Primaria)

        Return paraDevolver
    End Function


    ''' <summary>
    ''' Genera los ficheros PO con las traducciones del proyecto utilizando los idiomas
    ''' configurados en el objeto
    ''' </summary>
    Public Function ArchivoNET2ArchivoPO(ByVal eArchivoNET As NET.cFormulario) As Boolean
        ' Mensaje utilizado para enviar información mediante los eventos
        Dim elMensaje As String = ""

        ' Se lee el contenido de fichero con la versión antigua de la traducción, para utilizarla como
        ' base de traducción y evitar volver a traducir el texto ya traducido o el texto que fué
        ' corregido posteriormente
        If iConEventos Then RaiseEvent notificarMensaje("? Analizando traducciones previas de " & eArchivoNET.NombreFichero & "...")

        ' Diccionario donde se va a guardar el idioma y el contenido del fichero PO previo
        Dim traduccionesAntiguas As New Dictionary(Of idiomaLocalizacion, String)

        ' Se recorre cada uno de los idiomas de salida configurados y se carga en el diccionario
        ' de traducciones antiguas
        If iConEventos Then RaiseEvent notificarMaximo(TipoBarraProgreso.Secundaria, iIdiomas.Count + 1)
        For Each unIdioma As cIdioma In iIdiomas
            Try
                If iConEventos Then RaiseEvent notificarMensaje("< Obteniendo traducciones de la versión " & VersionTraduccion & " para " & unIdioma.strNombre & "[" & unIdioma.codigoLocalizacion & "]...")
                If iConEventos Then RaiseEvent notificarProgreso(TipoBarraProgreso.Secundaria, 0)

                Dim rutaVersionAntigua As String = iProyectoVB.carpetaTraducciones & unIdioma.strCodigoLocalizacion & "_" & VersionTraduccion & ".po"
                If File.Exists(rutaVersionAntigua) Then
                    Dim contenidoVersionAntigua As String = ""

                    Dim lectorVersionAntigua As New StreamReader(rutaVersionAntigua, System.Text.Encoding.UTF8)
                    contenidoVersionAntigua = lectorVersionAntigua.ReadToEnd
                    lectorVersionAntigua.Close()

                    ' Se añade la traduccin al diccionario
                    traduccionesAntiguas.Add(unIdioma.codigoLocalizacion, contenidoVersionAntigua)
                Else
                    If iConEventos Then RaiseEvent notificarMensaje("! ADVERTENCIA, no existe traducción previa a la versión " & VersionTraduccion & " para " & unIdioma.strNombre & " [" & unIdioma.strCodigoLocalizacion & "]...")
                End If
            Catch ex As Exception
                If iConEventos Then RaiseEvent notificarMensaje("! ERROR al obtenerlas traducciones de la versión " & VersionTraduccion & " para " & unIdioma.strNombre & " [" & unIdioma.strCodigoLocalizacion & "]...")
            End Try
        Next
        If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Secundaria)

        ' Se convierte todo el proyecto a UTF8 para evitar problemas de traduccion, para ello, se van
        ' leyendo todos los componentes del proyecto, se escriben en UTF8 en un fichero temporal, y se vuelven
        ' a copiar en el proyecto, guardando una copia del documento original en la carpeta _BACKUP en
        ' la misma carpeta donde se encuentra el proyecto.
        If iConEventos Then RaiseEvent notificarMensaje("# Realizando copia de seguridad de " & eArchivoNET.NombreFichero & "...")
        Try
            Dim rutaBackup As String = iProyectoVB.carpetaProyecto & "\_BACKUP\"
            If Not IO.Directory.Exists(rutaBackup) Then IO.Directory.CreateDirectory(rutaBackup)

            If eArchivoNET.RutaFormulario.Contains("\..\") Then
                rutaBackup = eArchivoNET.RutaFormulario & ".bak"
            Else
                rutaBackup &= eArchivoNET.RutaFormulario.Substring(iProyectoVB.carpetaProyecto.Length + 1) & ".bak"
            End If
            Ficheros.Copiar.copiarArchivo(eArchivoNET.RutaFormulario, rutaBackup, True, True)
        Catch ex As Exception
            elMensaje = "! ERROR al realizar la copia de seguridad de " & eArchivoNET.NombreFichero & "..."
            If iConEventos Then RaiseEvent notificarMensaje(elMensaje)
            Throw New Exception(elMensaje, ex)
        End Try

        ' Cambio del fichero a UTF-8, además, se crea el objeto Components para poder acceder a estos a la hora de traducir los objetos
        ' que no tiene representación sobre el formulario, accediendo a ellos mediante esta nueva propiedad (losComponentes)
        If iConEventos Then RaiseEvent notificarMensaje("# Convirtiendo " & eArchivoNET.NombreFichero & " a UFT-8...")
        Try
            Dim archivoTemporal As String = Ficheros.obtenerFicheroTemporal
            If IO.File.Exists(archivoTemporal) Then IO.File.Delete(archivoTemporal)
            Dim contenidoOriginal As String = File.ReadAllText(eArchivoNET.RutaFormulario, System.Text.Encoding.Default)

            ' Se realizan ajustes sobre el objeto Comonents para poder acceder a estos,
            ' cambiando la linean "End Class" por la nueva linea de componentes
            If contenidoOriginal.Contains("Private components As System.ComponentModel.IContainer") Then
                If Not contenidoOriginal.Contains("Public ReadOnly Property losComponentes As System.ComponentModel.ComponentCollection") Then
                    Dim losComponentes As String = Environment.NewLine & _
                                                   "Public ReadOnly Property losComponentes As System.ComponentModel.ComponentCollection" & Environment.NewLine & _
                                                   "    Get" & Environment.NewLine & _
                                                   "        If Me.components IsNot Nothing Then" & Environment.NewLine & _
                                                   "            Return Me.components.Components" & Environment.NewLine & _
                                                   "        else" & Environment.NewLine & _
                                                   "            Return Nothing" & Environment.NewLine & _
                                                   "        End If" & Environment.NewLine & _
                                                   "    End Get" & Environment.NewLine & _
                                                   "End Property" & Environment.NewLine & _
                                                   Environment.NewLine & _
                                                   "End Class"

                    contenidoOriginal = contenidoOriginal.Replace("End Class", losComponentes)
                End If
            End If

            ' Se copia el fichero en UTF sobreescribiendo el fichero origienal            
            File.WriteAllText(archivoTemporal, contenidoOriginal, System.Text.Encoding.UTF8)
            IO.File.Copy(archivoTemporal, eArchivoNET.RutaFormulario, True)

            ' Se elimina el archivo temporal utilizado para la conversión
            Try
                IO.File.Delete(archivoTemporal)
            Catch ex As Exception
            End Try
        Catch ex As Exception
            elMensaje = "# Error al convertir " & eArchivoNET.NombreFichero & " a UFT-8..."
            If iConEventos Then RaiseEvent notificarMensaje(elMensaje)
            Throw New Exception(elMensaje, ex)
        End Try

        ' Se cuentan cuantos elementos se van a procesar
        If iConEventos Then RaiseEvent notificarMensaje(vbTab & "? Obteniendo controles/cadneas a traducir...")

        ' + CONTAR CONTROLES
        Dim losControles As Dictionary(Of String, String) = obtenerControles(eArchivoNET, ProyectoTraductor.ControlesNET)
        Dim totalControles As Integer = losControles.Count

        ' + CONTAR MENSAJES
        Dim lasCadenas As Dictionary(Of String, String) = obtenerCadenas(eArchivoNET)
        Dim totalMensajes As Integer = lasCadenas.Count

        ' CONTAR IDIOMAS
        Dim totalIdiomas As Integer = iIdiomas.Count

        ' Solamente se realiza la traducción si hay algo que traducir
        If totalControles > 0 Or totalIdiomas > 0 AndAlso IO.File.Exists(eArchivoNET.RutaFormulario) Then
            ' Se obtiene el nombre del formulario para la generación de los nombres de los objetos
            ' a insertar el fichero PO. Los nombres de los formularios son únicos por proyecto, por
            ' lo que esta será la base del objeto que se añadirá al fichero PO para identificar 
            ' los controles y mensajes traducidos
            Dim NombreFormulario As String = eArchivoNET.NombreFormulario

            ' Se lee el contenido del formulario. Este formulario ya está convertido a UTF-8
            ' por lo que el Encodig se fija a este formato
            Dim elLector As StreamReader = New System.IO.StreamReader(eArchivoNET.RutaFormulario, System.Text.Encoding.UTF8)
            Dim elFicheroDesignerCompleto As String = elLector.ReadToEnd
            elLector.Close()

            ' Se crea el fichero HTML con las cadenas de textos a traducir
            Dim NombreFicheroIdiomaEntrada As String = iProyectoVB.carpetaTraducciones & IdiomaUso.strCodigoLocalizacion & ".html"
            If IO.File.Exists(NombreFicheroIdiomaEntrada) Then IO.File.Delete(NombreFicheroIdiomaEntrada)

            Dim elEscritor As New StreamWriter(NombreFicheroIdiomaEntrada, False, System.Text.Encoding.UTF8)
            elEscritor.WriteLine("<meta http-equiv=""Content-Type"" content=""text/html;charset=utf-8"">")
            elEscritor.WriteLine("<table border=1>")

            ' A medida que se detectan controles se van añadiendo a la lista de los nombres de los controles
            ' y el texto original, siempre y cuando este no se encuentre ya en la versión antigua de la
            ' traducción con el mismo texto, lo que significaría que la traducción no cambia y sigue siendo
            ' la misma, en caso contrario, se tiene que volver a traducir.
            If iConEventos Then RaiseEvent notificarMaximo(TipoBarraProgreso.Secundaria, iIdiomas.Count + 1)
            Dim DiccionarioTraducciones As New Dictionary(Of idiomaLocalizacion, List(Of cTraduccionIntermedia))
            For Each UnIdioma As cIdioma In iIdiomas
                If iConEventos Then RaiseEvent notificarProgreso(TipoBarraProgreso.Secundaria, 0)
                Try
                    DiccionarioTraducciones.Add(UnIdioma.codigoLocalizacion, New List(Of cTraduccionIntermedia))
                Catch ex As Exception
                End Try
            Next
            If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Secundaria)

            ' Controla el numero de elementos que se están traduciendo y si se encontró el propio formulario
            Dim elIndice As Long = 0
            Dim encontroFormulario As Boolean = False

            ' Se obitenen los patrones de controles que identifican a los formularios
            Dim patronesFormulario As List(Of String) = (From it As NET.cControl In ProyectoTraductor.ControlesNET _
                                                         Where it.esFormulario = True _
                                                         Select it.Tipo).ToList

            ' Se obitenen los patrones de controles que necesitan un UniqueName para identificarlos
            Dim patronesUniqueName As New Dictionary(Of String, String)
            Dim patronesAux = (From it As NET.cControl In ProyectoTraductor.ControlesNET _
                               Where it.conUniqueName = True _
                               Select it).ToList
            If patronesAux IsNot Nothing AndAlso patronesAux.Count > 0 Then
                For Each unPatron As NET.cControl In patronesAux
                    If Not String.IsNullOrEmpty(unPatron.rutaUniqueName) AndAlso Not patronesUniqueName.Keys.Contains(unPatron.rutaUniqueName) Then
                        patronesUniqueName.Add(unPatron.rutaUniqueName, unPatron.sufijoUniqueName)
                    End If
                Next
            End If

            If iConEventos Then RaiseEvent notificarMaximo(TipoBarraProgreso.Secundaria, losControles.Count + 1)
            For Each parControles As KeyValuePair(Of String, String) In losControles
                ' Nombre y tipo del control que se está analizando
                Dim nombreControl As String = parControles.Key
                Dim tipoControl As String = parControles.Value

                ' Se obtiene la información de las propiedades a traducir dependiendo del tipo del
                ' control. Si esta no se pudiera localizar se continua con el siguiente control
                Dim auxTipoControl As String = tipoControl
                If auxTipoControl.EndsWith("()") Then auxTipoControl = auxTipoControl.Substring(0, auxTipoControl.Length - 2)
                Dim infoControl As NET.cControl = (From it As NET.cControl In ProyectoTraductor.ControlesNET _
                                                   Where it.Tipo = auxTipoControl _
                                                   Select it).FirstOrDefault
                If infoControl Is Nothing Then Continue For

                ' Variables para el trabajo con el nombre y tipo
                Dim posInicio As Integer = 0
                Dim posFin As Integer = 0
                Dim uniqueName As String = ""

                ' Se verifica si el control que se está analizando es el formulario
                For Each unPatronFormulario As String In patronesFormulario
                    If parControles.Value.EndsWith(unPatronFormulario) Then
                        encontroFormulario = True
                    End If
                Next

                ' Se verifican los controles UniqueName
                For Each unPatronUnique As KeyValuePair(Of String, String) In patronesUniqueName
                    If parControles.Value.Contains(unPatronUnique.Key) Then
                        If String.IsNullOrEmpty(unPatronUnique.Value) Then
                            Try
                                Dim elPatron As String = unPatronUnique.Key
                                posInicio = elFicheroDesignerCompleto.IndexOf(elPatron)

                                If posInicio > 0 Then
                                    posFin = elFicheroDesignerCompleto.IndexOf("""", posInicio + elPatron.Length + 1)

                                    uniqueName = elFicheroDesignerCompleto.Substring(posInicio + elPatron.Length + 1, (posFin + 1) - (posInicio - 1))
                                    uniqueName = uniqueName.Replace("""", "").Trim
                                End If
                            Catch ex As Exception
                                Debugger.Break()
                                uniqueName = ""
                            End Try
                        Else
                            uniqueName = unPatronUnique.Value
                        End If
                    End If
                Next

                For Each unaPropiedad As NET.cPropiedad In infoControl.Propiedades
                    AnhadirControl(elFicheroDesignerCompleto, parControles, unaPropiedad, elEscritor, elIndice, NombreFormulario, DiccionarioTraducciones, traduccionesAntiguas, uniqueName)
                Next

                If iConEventos Then RaiseEvent notificarProgreso(TipoBarraProgreso.Secundaria, 0)
            Next
            If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Secundaria)

            ' Si no encontró el formulario se fuerza la búsqueda.
            ' A veces el propio formulario no se crea con New System.Windows.Forms.Form() 
            ' y el Texto aparece directamente bajo el patrón .Text = 
            If Not encontroFormulario Then
                Dim elPar As New KeyValuePair(Of String, String)("Me", "System.Windows.Forms.Form()")
                Dim laPropiedad As New NET.cPropiedad With {
                    .Propiedad = ".Text = ", _
                    .Sufijo = "_Form_Text"
                }
                AnhadirControl(elFicheroDesignerCompleto, elPar, laPropiedad, elEscritor, elIndice, NombreFormulario, DiccionarioTraducciones, traduccionesAntiguas, "")
            End If

            If iConEventos Then RaiseEvent notificarMaximo(TipoBarraProgreso.Secundaria, lasCadenas.Count + 1)
            For Each parCadenas As KeyValuePair(Of String, String) In lasCadenas
                elEscritor.Write("<tr>")
                elEscritor.Write("<td>_" & elIndice & "_</td>")
                elEscritor.Write("<td>" & Web.HTML.UTF2HTML(parCadenas.Value) & "</td>")
                elEscritor.WriteLine("</tr>")

                If iConEventos Then RaiseEvent notificarMensaje("[" & parCadenas.Key & "] <" & parCadenas.Value & ">")

                ' Se añadio el row a la tabla, por lo que se añade a la lista de controles coincidiendo con el índice
                For Each unIdioma As cIdioma In iIdiomas
                    Dim LaTraduccion As New cTraduccionIntermedia With {
                        .Indice = elIndice,
                        .NombreControl = NombreFormulario & "." & parCadenas.Key,
                        .Original = parCadenas.Value,
                        .Traduccion = ""
            }
                    DiccionarioTraducciones(unIdioma.codigoLocalizacion).Add(LaTraduccion)

                Next
                elIndice += 1
                If iConEventos Then RaiseEvent notificarProgreso(TipoBarraProgreso.Secundaria, 0)
            Next
            If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Secundaria)

            elEscritor.WriteLine("</table>")

            ' Utilizado para verificar que se terminó de cargar la página
            elEscritor.WriteLine(_END_OF_FILE)
            elEscritor.Close()

            If iConEventos Then RaiseEvent notificarMensaje("~ Enviando HTML al servidor FTP...")
            Dim NombreFicheroServidor As String = Aleatorios.cadenaAleatoria(8, True) & ".html"
            Dim ErrorSubida As Boolean = False
            Dim ContadorIntentos As Integer = 1
            Do
                If iConEventos Then RaiseEvent notificarMaximo(TipoBarraProgreso.Secundaria, 100)
                If iConEventos Then RaiseEvent notificarMensaje("~ Intento " & ContadorIntentos & " de 10...")
                System.Threading.Thread.Sleep(500)
                Try
                    'ToDo: Arreglar el upload file par amostrar el progreso
                    'UploadFile(NombreFicheroIdiomaEntrada, ConfiguracionNetwork.Servidor & ConfiguracionNetwork.Ruta & NombreFicheroServidor)
                    My.Computer.Network.UploadFile(NombreFicheroIdiomaEntrada, ConfiguracionNetwork.Servidor & ConfiguracionNetwork.Ruta & NombreFicheroServidor, ConfiguracionNetwork.Usuario, ConfiguracionNetwork.Clave, True, 2500)
                    ErrorSubida = False
                Catch ex As Exception
                    ErrorSubida = True
                End Try
                ContadorIntentos += 1
            Loop While (ContadorIntentos <= 10) And ErrorSubida
            If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Secundaria)

            ' Una vez que se ha subido el fichero al servidor, se recorre cada uno de los lenguaje sde salida
            ' pora completar los diccionarios con las traducciones que realiza el parseador            
            If iConEventos Then RaiseEvent notificarMaximo(TipoBarraProgreso.Secundaria, iIdiomas.Count + 1)
            For Each UnIdioma As cIdioma In iIdiomas
                If iConEventos Then RaiseEvent notificarMensaje("@ Realizando traducción de " & eArchivoNET.NombreFichero & " a " & UnIdioma.strNombre & " [" & UnIdioma.strCodigoLocalizacion & "]...")

                ' Se obtiene el body de la página traducido
                Dim url_Original As String = ConfiguracionNetwork.URLBase & NombreFicheroServidor
                Dim lasTraducciones As Dictionary(Of Long, String) = Motor.obtenerTraducciones(iConfiguracionNetwork, IdiomaUso, UnIdioma, url_Original, _END_OF_FILE)

                ' Se espera el tiempo configurado para el motor para evitar uso excesivo
                ' de CPU y del propio servicio
                System.Threading.Thread.Sleep(CType(Motor, Motor.cMotorBase).SleepTime)

                ' Se añaden las traducciones obtenidas al diccionario de traducciones para 
                ' exportarlos al fichero PO asociado al idioma traducido
                If lasTraducciones IsNot Nothing AndAlso lasTraducciones.Count > 0 Then
                    If iConEventos Then RaiseEvent notificarMaximo(TipoBarraProgreso.Secundaria, lasTraducciones.Count)
                    For Each unaTraduccion As KeyValuePair(Of Long, String) In lasTraducciones
                        DiccionarioTraducciones(UnIdioma.codigoLocalizacion)(unaTraduccion.Key).Traduccion = unaTraduccion.Value

                        If iConEventos Then RaiseEvent notificarProgreso(TipoBarraProgreso.Secundaria, 0)
                    Next
                    If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Secundaria)
                End If
            Next

            Dim ultimoTraducido As idiomaLocalizacion = IdiomaUso.codigoLocalizacion
            For Each UnIdioma As cIdioma In iIdiomas
                If UnIdioma.codigoLocalizacion <> Me.IdiomaUso.codigoLocalizacion Then
                    Dim NombreFicheroSalida As String = iProyectoVB.carpetaTraducciones & UnIdioma.strCodigoLocalizacion & ".po"
                    Dim elEscritorPO As New StreamWriter(NombreFicheroSalida, True, System.Text.Encoding.UTF8)

                    For Each UnaEntrada As cTraduccionIntermedia In DiccionarioTraducciones(UnIdioma.codigoLocalizacion)
                        ' Se comprueba si la traducción ya existía en la versión antigua y esta sigue siendo la misma, de
                        ' ser así, esta se ignora
                        If traduccionesAntiguas IsNot Nothing AndAlso traduccionesAntiguas.Keys.Contains(UnIdioma.codigoLocalizacion) Then
                            Dim IndiceCabecera As Integer = traduccionesAntiguas(UnIdioma.codigoLocalizacion).IndexOf("#: " & UnaEntrada.NombreControl)
                            If IndiceCabecera > 0 Then
                                Dim IndiceCuerpo As Integer = traduccionesAntiguas(UnIdioma.codigoLocalizacion).IndexOf("msgid """ & UnaEntrada.Original & """", IndiceCabecera)
                                If IndiceCuerpo > 0 Then
                                    Dim inicioMensajeAntiguo As Integer = traduccionesAntiguas(UnIdioma.codigoLocalizacion).IndexOf("msgstr ", IndiceCuerpo) + 8
                                    Dim finalMensajeAntiguo As Integer = traduccionesAntiguas(UnIdioma.codigoLocalizacion).IndexOf(Chr(10), inicioMensajeAntiguo) - 1
                                    UnaEntrada.Traduccion = traduccionesAntiguas(UnIdioma.codigoLocalizacion).Substring(inicioMensajeAntiguo, finalMensajeAntiguo - (inicioMensajeAntiguo + 1)).Trim
                                End If
                            End If
                        End If

                        elEscritorPO.WriteLine("#: " & UnaEntrada.NombreControl)
                        elEscritorPO.WriteLine("msgid """ & Web.HTML.ANSI2UTF8(UnaEntrada.Original) & """")
                        elEscritorPO.WriteLine("msgstr """ & Web.HTML.HTML2UTF(Web.HTML.ANSI2UTF8(UnaEntrada.Traduccion)) & """")
                        elEscritorPO.WriteLine()

                        If iConEventos Then RaiseEvent notificarMensaje("* [" & IdiomaUso.strCodigoLocalizacion & "] -> [" & UnIdioma.strCodigoLocalizacion & "] " & UnaEntrada.Original & " -> " & UnaEntrada.Traduccion)
                        If iConEventos Then RaiseEvent notificarProgreso(TipoBarraProgreso.Secundaria, 0)
                    Next

                    ' Se escribe un texto de final de documento para evitar problemas de EOF cuando se trabaja con el fichero
                    elEscritorPO.WriteLine("#: EOF")
                    elEscritorPO.WriteLine("msgid """ & "EOF" & """")
                    elEscritorPO.WriteLine("msgstr """ & "EOF" & """")
                    elEscritorPO.WriteLine()

                    elEscritorPO.Close()
                    ultimoTraducido = UnIdioma.codigoLocalizacion
                End If
            Next

            ' Finalmente se añade el archivo del idioma original con las traducciones con los mismos textos            
            Dim NombreFicheroSalidaOriginal As String = iProyectoVB.carpetaTraducciones & IdiomaUso.strCodigoLocalizacion & ".po"
            Dim elEscritorPOOriginal As New StreamWriter(NombreFicheroSalidaOriginal, True, System.Text.Encoding.UTF8)

            For Each UnaEntrada As cTraduccionIntermedia In DiccionarioTraducciones(ultimoTraducido)
                elEscritorPOOriginal.WriteLine("#: " & UnaEntrada.NombreControl)
                elEscritorPOOriginal.WriteLine("msgid """ & Web.HTML.ANSI2UTF8(UnaEntrada.Original) & """")
                elEscritorPOOriginal.WriteLine("msgstr """ & Web.HTML.ANSI2UTF8(UnaEntrada.Original) & """")
                elEscritorPOOriginal.WriteLine()

                If iConEventos Then RaiseEvent notificarProgreso(TipoBarraProgreso.Secundaria, 0)
            Next

            elEscritorPOOriginal.Close()

            ' Se elimina el fichero HTML utilizado para realizar la traducción
            If IO.File.Exists(NombreFicheroIdiomaEntrada) Then
                Try
                    IO.File.Delete(NombreFicheroIdiomaEntrada)
                Catch ex As Exception
                End Try
            End If
        End If

        ' Se avisa que se finalió la conversión del fichero
        If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Secundaria)
        Return True
    End Function
#End Region

#Region " METODOS AUXILIARES PO "
    ''' <summary>
    ''' Escribe la cabecera del fichero PO y lo configura para la utilización con el sistema
    ''' de traducciones automáticas
    ''' </summary>
    Public Sub EscribirCabeceraPO(ByVal eEscritor As StreamWriter, _
                                  ByVal eIdioma As String)
        Try
            If eEscritor IsNot Nothing AndAlso iProyectoVB IsNot Nothing Then
                With eEscritor
                    .WriteLine("msgid " & """" & """")
                    .WriteLine("msgstr " & """" & """")
                    .WriteLine("""" & "Project-Id-Version: " & iProyectoVB.Ensamblado & "\n" & """")
                    .WriteLine("""" & "Report-Msgid-Bugs-To: " & iProyectoTraductor.TraductorEmail & "\n" & """")
                    .WriteLine("""" & "POT-Creation-Date: " & Now & "\n" & """")
                    .WriteLine("""" & "PO-Revision-Date: " & Now & "\n" & """")
                    .WriteLine("""" & "Last-Translator: " & iProyectoTraductor.TraductorNombre & "\n" & """")
                    .WriteLine("""" & "Language-Team: " & iProyectoTraductor.TraductorEquipo & "\n" & """")
                    .WriteLine("""" & "MIME-Version: 1.0\n" & """")
                    .WriteLine("""" & "Content-Type: text/plain; charset=UTF-8\n" & """")
                    .WriteLine("""" & "Content-Transfer-Encoding: 8bit\n" & """")
                    .WriteLine("""" & "X-Poedit-SourceCharset: utf-8\n" & """")
                    .WriteLine("""" & "X-Poedit-KeywordsList: esc_html__;esc_html_e;esc_attr__;esc_attr_e;__;_e\n" & """")
                    .WriteLine("""" & "X-Poedit-Basepath: .\n" & """")
                    .WriteLine("""" & "Language: " & eIdioma & "\n" & """")
                    .WriteLine("""" & "X-Generator: Poedit 1.5.5\n" & """")
                    .WriteLine("""" & "X-Poedit-SearchPath-0: .\n" & """")
                    .WriteLine("""" & "X-Poedit-SearchPath-1: ..\n" & """")
                    .WriteLine()
                End With
            Else
                If Log._LOG_ACTIVO Then Log.escribirLog("ERROR al tratar de escribir la cabecera del archivo PO...", , New StackTrace(0, True))
            End If
        Catch ex As Exception
            If Log._LOG_ACTIVO Then Log.escribirLog("ERROR al tratar de escribir la cabecera del archivo PO...", , New StackTrace(0, True))
        End Try
    End Sub
#End Region

#Region " UPLOAD"
    Public Sub UploadFile(ByVal eLocal As String, _
                          ByVal eRemoto As String)
        Try
            If iConEventos Then RaiseEvent notificarMaximo(TipoBarraProgreso.Secundaria, 100)
            Me.fileUploader = New System.Net.WebClient


            Dim Credenciales As New NetworkCredential(iConfiguracionNetwork.Usuario, iConfiguracionNetwork.Clave)
            Me.fileUploader.Credentials = Credenciales
            Me.fileUploader.UploadFileAsync(New Uri(eRemoto), eLocal)
        Catch ex As Exception
            If Log._LOG_ACTIVO Then Log.escribirLog("Error al iniciar la subida de un fichero...", ex, New StackTrace(0, True))
        End Try
    End Sub

    Public Sub UpdateProgressBar(ByVal sender As Object, ByVal e As UploadProgressChangedEventArgs) Handles fileUploader.UploadProgressChanged
        If iConEventos Then RaiseEvent notificarProgreso(TipoBarraProgreso.Secundaria, CInt(Math.Round(e.ProgressPercentage)) * 2)
    End Sub

    Public Sub UploadComplete(ByVal sender As Object, ByVal e As System.ComponentModel.AsyncCompletedEventArgs) Handles fileUploader.UploadFileCompleted
        Try
            If iConEventos Then RaiseEvent notificarFinalizacion(TipoBarraProgreso.Secundaria)

            Do
                Application.DoEvents()
            Loop While fileUploader.IsBusy

            If fileUploader IsNot Nothing Then Me.fileUploader.Dispose()
            fileUploader = Nothing
        Catch ex As Exception
            If Log._LOG_ACTIVO Then Log.escribirLog("Error al finalizar la subida de un fichero...", ex, New StackTrace(0, True))
        Finally
            iSubiendo = False
        End Try
    End Sub
#End Region





    ''' <summary>
    ''' Se encarga de añadir un control/componente/texto a la lista de controles 
    ''' </summary>
    Private Function AnhadirControl(ByVal eFicheroDesignerCompleto As String, _
                                    ByVal eParControles As KeyValuePair(Of String, String), _
                                    ByVal ePropiedad As NET.cPropiedad, _
                                    ByVal eEscritor As StreamWriter, _
                                    ByRef eIndice As Long, _
                                    ByVal eFormulario As String, _
                                    ByRef eDiccionarioTraducciones As Dictionary(Of idiomaLocalizacion, List(Of cTraduccionIntermedia)), _
                                    ByVal eContenidoAntiguo As Dictionary(Of idiomaLocalizacion, String), _
                                    Optional ByVal eUniqueName As String = "") As Boolean
        Dim posInicio As Integer = -1
        Dim posFin As Integer = -1
        Dim elTextoTraducir As String = ""
        Dim PostNombre As String = ""

        posInicio = eFicheroDesignerCompleto.IndexOf(eParControles.Key & ePropiedad.Propiedad)
        If posInicio > 0 Then PostNombre = ePropiedad.Sufijo

        If posInicio >= 0 Then
            Dim arrayIdiomas(Idiomas.Count) As String
            posFin = eFicheroDesignerCompleto.IndexOf(vbCrLf, posInicio + 1)

            Dim laSubCadena As String = eFicheroDesignerCompleto.Substring(posInicio, posFin - posInicio)
            laSubCadena = laSubCadena.Trim

            While laSubCadena.EndsWith(" & _")
                laSubCadena = laSubCadena.Replace(""" & _", "")
                posInicio = posFin + 1
                posFin = eFicheroDesignerCompleto.IndexOf(vbCrLf, posInicio + 1)
                Dim SiguienteTrozo As String = eFicheroDesignerCompleto.Substring(posInicio, posFin - posInicio).Trim
                If SiguienteTrozo.StartsWith("""") Then SiguienteTrozo = SiguienteTrozo.Substring(1, SiguienteTrozo.Length - 1)
                laSubCadena &= SiguienteTrozo
            End While

            Dim posIniComillas As Integer = laSubCadena.IndexOf("""") + 1
            If posIniComillas >= 0 Then
                elTextoTraducir = laSubCadena.Substring(posIniComillas)
                elTextoTraducir = elTextoTraducir.Substring(0, elTextoTraducir.Length - 1).Replace("&", "")

                Dim resultadoTraduccion As String = ""

                With eEscritor
                    .Write("<tr>")
                    .Write("<td>" & eIndice & "</td>")
                    .Write("<td>" & Web.HTML.UTF2HTML(elTextoTraducir) & "</td>")
                    .WriteLine("</tr>")
                End With

                Dim nombreControl As String = eUniqueName
                If String.IsNullOrEmpty(nombreControl) Then
                    If eParControles.Key <> "Me" Then
                        nombreControl = eParControles.Key.Substring(3)
                    Else
                        nombreControl = eFormulario
                    End If
                End If

                If iConEventos Then RaiseEvent notificarMensaje("[" & nombreControl & " : " & eParControles.Value & "] <" & elTextoTraducir & ">")

                ' Se añadio el row a la tabla, por lo que se añade a la lista de controles coincidiendo con el índice
                For Each unIdioma As cIdioma In iIdiomas
                    Dim LaTraduccion As New cTraduccionIntermedia With {
                        .Indice = eIndice,
                        .NombreControl = eFormulario & "." & nombreControl & PostNombre,
                        .Original = elTextoTraducir,
                        .Traduccion = ""
                }
                    eDiccionarioTraducciones(unIdioma.codigoLocalizacion).Add(LaTraduccion)
                Next

                ' Se pasa al siguiente control y se devuelve un True indicando que 
                ' se encontró el patrón y se añadió al ficheor de traducciones
                eIndice += 1
                Return True
            End If
        End If

        ' Si el código llega hasta aquí es que no se pudo añadir el control ya que 
        ' no se encontró el patrón de búsqueda. Es necesario saber si se añadió
        ' o no ya que aunque un control no tenga traducción puede que si lo tengan
        ' sus hijos, en tal caso, se debe añadir igual sin texto
        Return False
    End Function



    ''' <summary>
    ''' Obitiene los controles que se pueden traducir o el sistema de traducción automática puede traducir
    ''' a partir de la configuración de los controles que se quieren traducir
    ''' </summary>
    ''' <remarks>Diccionario utilizando el Key como el nombre del control y el Value como el tipo del control</remarks>
    Private Function obtenerControles(ByVal eArchivoNET As NET.cFormulario, _
                                      ByVal eObjetosNET As List(Of NET.cControl)) As Dictionary(Of String, String)
        Dim paraDevolver As New Dictionary(Of String, String)

        If IO.File.Exists(eArchivoNET.RutaFormulario) Then
            Dim elLector As New System.IO.StreamReader(eArchivoNET.RutaFormulario, System.Text.Encoding.UTF8)
            Dim laCadena As String = ""

            ' Se recorre todo el contenido del archiov NET para tratar de localizar todos los objetos
            ' que se pueden traducir. A medida que se van detectando se añaden al diccionario
            Do
                laCadena = elLector.ReadLine.Trim

                If Not String.IsNullOrEmpty(laCadena) AndAlso laCadena.Contains(" = New ") Then
                    For Each unObjetoNet As NET.cControl In eObjetosNET
                        If laCadena.EndsWith(unObjetoNet.Tipo) Or laCadena.EndsWith(unObjetoNet.Tipo & "()") Then
                            Try
                                Dim nombreControl As String = laCadena.Substring(0, laCadena.IndexOf("=")).Trim
                                Dim tipoComponente As String = laCadena.Substring(laCadena.IndexOf("=") + 5).Trim
                                tipoComponente = tipoComponente.Replace("Me.components", "")

                                If Not String.IsNullOrEmpty(nombreControl) AndAlso Not String.IsNullOrEmpty(tipoComponente) AndAlso Not paraDevolver.Keys.Contains(nombreControl) Then
                                    paraDevolver.Add(nombreControl, tipoComponente)
                                End If
                            Catch ex As Exception
#If DEBUG Then
                                Debugger.Break()
#End If
                            End Try
                        End If
                    Next
                End If
            Loop Until elLector.EndOfStream
            elLector.Close()
        End If

        Return paraDevolver
    End Function

    ''' <summary>
    ''' Obitnene las cadenas de texto que el sistema es capaz de traducir
    ''' </summary>
    Private Function obtenerCadenas(ByVal eArchivoVB As NET.cFormulario) As Dictionary(Of String, String)
        Dim paraDevolver As New Dictionary(Of String, String)
        Dim encontroInicio As Boolean = False

        Dim laRutaCodigoFuente As String = eArchivoVB.RutaFormulario.Substring(0, eArchivoVB.RutaFormulario.Length - 11) & "vb"
        If System.IO.File.Exists(laRutaCodigoFuente) Then
            Dim elReader As New System.IO.StreamReader(laRutaCodigoFuente, System.Text.Encoding.UTF8)
            Dim laCadena As String
            Dim laKey As Integer = 0

            Do
                laCadena = elReader.ReadLine.Trim

                If encontroInicio Then
                    If laCadena = "}" Then
                        Exit Do
                    Else
                        If laCadena <> "" AndAlso laCadena <> """""" Then
                            laCadena = laCadena.Substring(1)

                            Dim elValor As String = laCadena.Split("""")(0)

                            paraDevolver.Add("_" & laKey, elValor)

                            laKey += 1
                        End If
                    End If
                Else
                    If laCadena = "Public Property theMessages As String() = { _" Then
                        encontroInicio = True
                    End If
                End If
            Loop Until elReader.EndOfStream
        End If

        Return paraDevolver
    End Function


End Class
