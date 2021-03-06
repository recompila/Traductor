﻿
Namespace Motor
    Public Class cMotorOnlineTranslator
        Inherits cMotorBase
        Implements IMotorTraduccion

#Region " PROPIEDADES "
        ''' <summary>
        ''' Tipo del motor de traducción
        ''' </summary>
        Public Overrides ReadOnly Property Tipo As motorTraduccion
            Get
                Return motorTraduccion.OnlineTranslator
            End Get
        End Property

        ''' <summary>
        ''' Nombre del motor de traducción
        ''' </summary>
        Public ReadOnly Property Nombre As String Implements IMotorTraduccion.Nombre
            Get
                Return "Online-Translator"
            End Get
        End Property

        ''' <summary>
        ''' URI utilizada para la traducción de la página HTML
        ''' </summary>
        Public ReadOnly Property URI As String Implements IMotorTraduccion.URI
            Get
                Return iURI
            End Get
        End Property
        Private iURI As String = "http://www.online-translator.com/url/translation/?autolink=yes&direction=@entrada@@salida@&template=General&sourceURL=http%3a%2f%2f@URI@"

        ''' <summary>
        ''' Distintas combinaciones que puede realizar el traductor con un idioma de entrada
        ''' </summary>
        Public ReadOnly Property TiposTraduccion As Dictionary(Of idiomaLocalizacion, List(Of idiomaLocalizacion)) Implements IMotorTraduccion.TiposTraduccion
            Get
                If iTiposTraduccion Is Nothing Then
                    iTiposTraduccion = New Dictionary(Of idiomaLocalizacion, List(Of idiomaLocalizacion))
                    With iTiposTraduccion
                        .Add(idiomaLocalizacion.es_ES, New List(Of idiomaLocalizacion) From {idiomaLocalizacion.en_US, idiomaLocalizacion.gl_GL, idiomaLocalizacion.de_DE, idiomaLocalizacion.pt_PT, idiomaLocalizacion.it_IT, idiomaLocalizacion.fr_FR, idiomaLocalizacion.ca_CA, idiomaLocalizacion.eu_EU, idiomaLocalizacion.el_EL, idiomaLocalizacion.ja_JA, idiomaLocalizacion.ru_RU, idiomaLocalizacion.ro_RO, idiomaLocalizacion.cz_CZ, idiomaLocalizacion.bg_BG, idiomaLocalizacion.hr_HR, idiomaLocalizacion.da_DA, idiomaLocalizacion.nl_NL, idiomaLocalizacion.fi_FI, idiomaLocalizacion.hu_HU, idiomaLocalizacion.sv_SV, idiomaLocalizacion.tr_TR})
                    End With
                End If
                Return iTiposTraduccion
            End Get
        End Property
        Private iTiposTraduccion As Dictionary(Of idiomaLocalizacion, List(Of idiomaLocalizacion)) = Nothing
#End Region

#Region " EVENTOS "
        Public Event notificarMensaje(eMensaje As String) Implements IMotorTraduccion.notificarMensaje
#End Region


#Region " CONSTRUCTORES "
        Public Sub New()
            MyBase.SleepTime = 2000
        End Sub
#End Region

#Region " METODOS PRIVADOS "
        Private Function obtenerIdentifiadorIdioma(ByVal eCodigoIdioma As idiomaLocalizacion) As String
            Dim paraDevolver As String = ""

            Select Case eCodigoIdioma
                Case idiomaLocalizacion.es_ES
                    paraDevolver = "s"

                Case idiomaLocalizacion.de_DE
                    paraDevolver = "g"

                Case idiomaLocalizacion.fr_FR
                    paraDevolver = "f"

                Case idiomaLocalizacion.en_US
                    paraDevolver = "e"

                Case idiomaLocalizacion.ru_RU
                    paraDevolver = "r"
            End Select

            Return paraDevolver
        End Function
#End Region

#Region " METODOS "
        ''' <param name="eDatosConexion">Datos de acceso al servidor FTP/HTTP</param>
        ''' <param name="eIdiomaEntrada">Idioma de entrada para la traducción</param>
        ''' <param name="eIdiomaSalida">Idioma de salida para la traducción</param>
        ''' <param name="eURL">URL donde se tiene que realizar la traducción</param>
        ''' <returns>URL formateada para poder realizar la traducción</returns>
        Public Function obtenerTraducciones(ByVal eDatosConexion As cConfiguracionNetwork, _
                                            ByVal eIdiomaEntrada As cIdioma, _
                                            ByVal eIdiomaSalida As cIdioma, _
                                            ByVal eURL As String, _
                                            ByVal eTagEOF As String) As Dictionary(Of Long, String) Implements IMotorTraduccion.obtenerTraducciones
            Dim paraDevolver As New Dictionary(Of Long, String)

            'paraDevolver = paraDevolver.Replace("@entrada@", obtenerIdentifiadorIdioma(eIdiomaEntrada.codigoLocalizacion))
            'paraDevolver = paraDevolver.Replace("@salida@", obtenerIdentifiadorIdioma(eIdiomaSalida.codigoLocalizacion))
            'paraDevolver = paraDevolver.Replace("@URI@", eURL.Replace("http://", ""))

            Return paraDevolver
        End Function
#End Region

#Region " SOBRECARGAS "
        ''' <summary>
        ''' Sobrecarga del método toString para mostrar el nombre del traductor
        ''' </summary>
        Public Overrides Function ToString() As String
            Return Me.Nombre
        End Function
#End Region
    End Class
End Namespace