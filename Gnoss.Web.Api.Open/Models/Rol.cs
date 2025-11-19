using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Es.Riam.Gnoss.AD.ServiciosGenerales;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Gnoss.Web.Api.Open.Controller;

namespace Gnoss.Web.Api.Open.Models
{
    public class ParamsRoleCommunity
    {
        /// <summary>
        /// Role identifier
        /// </summary>
        public Guid? rol_id { get; set; }
        /// <summary>
        /// Short name of the community to add the role
        /// </summary>
        public string community_short_name { get; set; }

        /// <summary>
        /// Community identifier
        /// </summary>
        public Guid? community_id { get; set; }

        /// <summary>
        /// Rol name
        /// </summary>
        public string pNombre { get; set; }

        /// <summary>
        /// Community description
        /// </summary>
        public string pDescripcion { get; set; }

        /// <summary>
        /// Community scope (community/ecosystem)
        /// </summary>
        public AmbitoRol pAmbito { get; set; }

        /// <summary>
        /// Permissions 
        /// </summary>
        public PermisosDTO pPermisos { get; set; }

        /// <summary>
        /// Resources permissions 
        /// </summary>
        public PermisosRecursosDTO pPermisosRecursos { get; set; }

        /// <summary>
        /// Ecosystem permissions 
        /// </summary>
        public PermisosEcosistemaDTO pPermisosEcosistema { get; set; }

        /// <summary>
        /// Content permissions 
        /// </summary>
        public PermisosContenidosDTO pPermisosContenidos { get; set; }

        /// <summary>
        /// Semantic Resources 
        /// </summary>
        public Dictionary<Guid, DiccionarioDePermisos> pPermisosRecursosSemanticos { get; set; }
    }




    public class PermisosDTO
    {
        // COMUNIDAD
        [Description("DESCPERMISOINFOGENERAL")]
        [Section("COMUNIDAD")]
        public bool GestionarInformacionGeneral { get; set; }

        [Description("DESCPERMISOFLUJOS")]
        [Section("COMUNIDAD")]
        public bool GestionarFlujos { get; set; }

        [Description("DESCPERMISOINTERACCIONESSOCIALES")]
        [Section("COMUNIDAD")]
        public bool GestionarInteraccionesSociales { get; set; }

        [Description("DESCPERMISOMIEMBROS")]
        [Section("COMUNIDAD")]
        public bool GestionarMiembros { get; set; }

        [Description("DESCPERMISOSOLICITUDESGRUPO")]
        [Section("COMUNIDAD")]
        public bool GestionarSolicitudesDeAccesoAGrupo { get; set; }

        [Description("DESCPERMISONIVELESCERTIFICACION")]
        [Section("COMUNIDAD")]
        public bool GestionarNivelesDeCertificacion { get; set; }

        [Description("DESCPERMISOROLES")]
        [Section("COMUNIDAD")]
        public bool GestionarRolesYPermisos { get; set; }

        // ESTRUCTURA
        [Description("DESCPERMISOPESOSAUTOCOMPLETAR")]
        [Section("ESTRUCTURA")]
        public bool GestionarPesosAutocompletado { get; set; }

        [Description("DESCPERMISOREDIRECCIONES")]
        [Section("ESTRUCTURA")]
        public bool GestionarRedirecciones { get; set; }

        // CONFIGURACIÓN
        [Description("DESCPERMISOOAUTH")]
        [Section("CONFIGURACION")]
        public bool DescargarConfiguracionOAuth { get; set; }

        [Description("DESCPERMISOCOOKIES")]
        [Section("CONFIGURACION")]
        public bool GestionarCookies { get; set; }

        [Description("DESCPERMISOFTP")]
        [Section("CONFIGURACION")]
        public bool AccederAlFTP { get; set; }

        [Description("DESCPERMISOTRADUCCIONES")]
        [Section("CONFIGURACION")]
        public bool GestionarTraducciones { get; set; }

        [Description("DESCPERMISODATOSEXTRA")]
        [Section("CONFIGURACION")]
        public bool GestionarDatosExtraRegistro { get; set; }

        [Description("DESCPERMISOTRAZAS")]
        [Section("CONFIGURACION")]
        public bool GestionarTrazas { get; set; }

        [Description("DESCPERMISOCONFIGURACIONES")]
        [Section("CONFIGURACION")]
        public bool GestionarConfiguraciones { get; set; }

        [Description("DESCPERMISOCACHE")]
        [Section("CONFIGURACION")]
        public bool GestionarCache { get; set; }

        [Description("DESCPERMISOSEO")]
        [Section("CONFIGURACION")]
        public bool AdministrarSEOYGoogleAnalytics { get; set; }

        [Description("DESCPERMISOESTADISTICAS")]
        [Section("CONFIGURACION")]
        public bool AccederAEstadisticasDeLaComunidad { get; set; }

        [Description("DESCPERMISOCLAUSULAS")]
        [Section("CONFIGURACION")]
        public bool GestionarClausulasDeRegistro { get; set; }

        [Description("DESCPERMISOCORREO")]
        [Section("CONFIGURACION")]
        public bool GestionarBuzonDeCorreo { get; set; }

        [Description("DESCPERMISOSERVICIOSEXTERNOS")]
        [Section("CONFIGURACION")]
        public bool GestionarServiciosExternos { get; set; }

        [Description("DESCPERMISOESTADOSERVICIOS")]
        [Section("CONFIGURACION")]
        public bool AccederAlEstadoDeLosServicios { get; set; }

        [Description("DESCPERMISOOPCIONESMETA")]
        [Section("CONFIGURACION")]
        public bool GestionarOpcionesDelMetaadministrador { get; set; }

        [Description("DESCPERMISOEVENTOS")]
        [Section("CONFIGURACION")]
        public bool GestionarEventosExternos { get; set; }

        // GRAFO
        [Description("DESCPERMISOSPARQL")]
        [Section("GRAFO")]
        public bool AccesoSparqlEndpoint { get; set; }

        [Description("DESCPERMISOCARGAMASIVA")]
        [Section("GRAFO")]
        public bool ConsultarCargasMasivas { get; set; }

        [Description("DESCPERMISOBORRADOMASIVO")]
        [Section("GRAFO")]
        public bool EjecutarBorradoMasivo { get; set; }

        [Description("DESCPERMISOSUGERENCIASBUSQUEDA")]
        [Section("GRAFO")]
        public bool GestionarSugerenciasDeBusqueda { get; set; }

        [Description("DESCPERMISOCONTEXTOS")]
        [Section("GRAFO")]
        public bool GestionarInformacionContextual { get; set; }

        // DESCUBRIMIENTO
        [Description("DESCPERMISOSEARCHPERSONALIZADO")]
        [Section("DESCUBRIMIENTO")]
        public bool GestionarParametrosDeBusquedaPersonalizados { get; set; }

        [Description("DESCPERMISOMAPA")]
        [Section("DESCUBRIMIENTO")]
        public bool GestionarMapa { get; set; }

        [Description("DESCPERMISOGRAFICOS")]
        [Section("DESCUBRIMIENTO")]
        public bool AdministrarGraficos { get; set; }

        // APARIENCIA
        [Description("DESCPERMISOVISTAS")]
        [Section("APARIENCIA")]
        public bool GestionarVistas { get; set; }

        // IC
        [Description("DESCPERMISOIC")]
        [Section("IC")]
        public bool GestionarIntegracionContinua { get; set; }

        // MANTENIMIENTO
        [Description("DESCPERMISOREPROCESAR")]
        [Section("MANTENIMIENTO")]
        public bool EjecutarReprocesadosDeRecursos { get; set; }

        // APLICACIONES
        [Description("DESCPERMISOAPLICACIONES")]
        [Section("APLICACIONES")]
        public bool GestionarAplicacionesEspecificas { get; set; }

        /// <summary>
        /// Convierte los permisos booleanos a un valor ulong
        /// </summary>
        public ulong ToUlong()
        {
            ulong permisos = 0;

            if (GestionarInformacionGeneral) permisos |= 1;
            if (GestionarFlujos) permisos |= 2;
            if (GestionarInteraccionesSociales) permisos |= 4;
            if (GestionarMiembros) permisos |= 8;
            if (GestionarSolicitudesDeAccesoAGrupo) permisos |= 16;
            if (GestionarNivelesDeCertificacion) permisos |= 32;
            if (GestionarPesosAutocompletado) permisos |= 64;
            if (GestionarRedirecciones) permisos |= 128;
            if (DescargarConfiguracionOAuth) permisos |= 256;
            if (GestionarCookies) permisos |= 512;
            if (AccederAlFTP) permisos |= 1024;
            if (GestionarTraducciones) permisos |= 2048;
            if (GestionarDatosExtraRegistro) permisos |= 4096;
            if (GestionarTrazas) permisos |= 8192;
            if (GestionarConfiguraciones) permisos |= 16384;
            if (GestionarCache) permisos |= 32768;
            if (AdministrarSEOYGoogleAnalytics) permisos |= 65536;
            if (AccederAEstadisticasDeLaComunidad) permisos |= 131072;
            if (GestionarClausulasDeRegistro) permisos |= 262144;
            if (GestionarBuzonDeCorreo) permisos |= 524288;
            if (GestionarServiciosExternos) permisos |= 1048576;
            if (AccederAlEstadoDeLosServicios) permisos |= 2097152;
            if (GestionarOpcionesDelMetaadministrador) permisos |= 4194304;
            if (GestionarEventosExternos) permisos |= 8388608;
            if (AccesoSparqlEndpoint) permisos |= 16777216;
            if (ConsultarCargasMasivas) permisos |= 33554432;
            if (EjecutarBorradoMasivo) permisos |= 67108864;
            if (GestionarSugerenciasDeBusqueda) permisos |= 134217728;
            if (GestionarInformacionContextual) permisos |= 268435456;
            if (GestionarParametrosDeBusquedaPersonalizados) permisos |= 536870912;
            if (GestionarMapa) permisos |= 1073741824;
            if (AdministrarGraficos) permisos |= 2147483648;
            if (GestionarVistas) permisos |= 4294967296;
            if (GestionarIntegracionContinua) permisos |= 8589934592;
            if (EjecutarReprocesadosDeRecursos) permisos |= 17179869184;
            if (GestionarAplicacionesEspecificas) permisos |= 34359738368;
            if (GestionarRolesYPermisos) permisos |= 68719476736;

            return permisos;
        }

        public static PermisosDTO FromUlong(ulong permisos)
        {
            return new PermisosDTO
            {
                GestionarInformacionGeneral = (permisos & 1) != 0,
                GestionarFlujos = (permisos & 2) != 0,
                GestionarInteraccionesSociales = (permisos & 4) != 0,
                GestionarMiembros = (permisos & 8) != 0,
                GestionarSolicitudesDeAccesoAGrupo = (permisos & 16) != 0,
                GestionarNivelesDeCertificacion = (permisos & 32) != 0,
                GestionarPesosAutocompletado = (permisos & 64) != 0,
                GestionarRedirecciones = (permisos & 128) != 0,
                DescargarConfiguracionOAuth = (permisos & 256) != 0,
                GestionarCookies = (permisos & 512) != 0,
                AccederAlFTP = (permisos & 1024) != 0,
                GestionarTraducciones = (permisos & 2048) != 0,
                GestionarDatosExtraRegistro = (permisos & 4096) != 0,
                GestionarTrazas = (permisos & 8192) != 0,
                GestionarConfiguraciones = (permisos & 16384) != 0,
                GestionarCache = (permisos & 32768) != 0,
                AdministrarSEOYGoogleAnalytics = (permisos & 65536) != 0,
                AccederAEstadisticasDeLaComunidad = (permisos & 131072) != 0,
                GestionarClausulasDeRegistro = (permisos & 262144) != 0,
                GestionarBuzonDeCorreo = (permisos & 524288) != 0,
                GestionarServiciosExternos = (permisos & 1048576) != 0,
                AccederAlEstadoDeLosServicios = (permisos & 2097152) != 0,
                GestionarOpcionesDelMetaadministrador = (permisos & 4194304) != 0,
                GestionarEventosExternos = (permisos & 8388608) != 0,
                AccesoSparqlEndpoint = (permisos & 16777216) != 0,
                ConsultarCargasMasivas = (permisos & 33554432) != 0,
                EjecutarBorradoMasivo = (permisos & 67108864) != 0,
                GestionarSugerenciasDeBusqueda = (permisos & 134217728) != 0,
                GestionarInformacionContextual = (permisos & 268435456) != 0,
                GestionarParametrosDeBusquedaPersonalizados = (permisos & 536870912) != 0,
                GestionarMapa = (permisos & 1073741824) != 0,
                AdministrarGraficos = (permisos & 2147483648) != 0,
                GestionarVistas = (permisos & 4294967296) != 0,
                GestionarIntegracionContinua = (permisos & 8589934592) != 0,
                EjecutarReprocesadosDeRecursos = (permisos & 17179869184) != 0,
                GestionarAplicacionesEspecificas = (permisos & 34359738368) != 0,
                GestionarRolesYPermisos = (permisos & 68719476736) != 0
            };
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SectionAttribute : Attribute
    {
        public string Section { get; }

        public SectionAttribute(string section)
        {
            Section = section;
        }
    }





    //PERMISOS DE CONTENIDOS
    public class PermisosContenidosDTO
    {
        // COMUNIDAD
        [Description("DESCPERMISOVERCATEGORIA")]
        [Section("COMUNIDAD")]
        public bool VerCategorias { get; set; }

        [Description("DESCPERMISOANYADIRCATEGORIA")]
        [Section("COMUNIDAD")]
        public bool AnyadirCategoria { get; set; }

        [Description("DESCPERMISOEDITARCATEGORIA")]
        [Section("COMUNIDAD")]
        public bool ModificarCategoria { get; set; }

        [Description("DESCPERMISOELIMINARCATEGORIA")]
        [Section("COMUNIDAD")]
        public bool EliminarCategoria { get; set; }

        // ESTRUCTURA
        [Description("DESCPERMISOVERPAGINA")]
        [Section("ESTRUCTURA")]
        public bool VerPagina { get; set; }

        [Description("DESCPERMISOCREARPAGINA")]
        [Section("ESTRUCTURA")]
        public bool CrearPagina { get; set; }

        [Description("DESCPERMISOPUBLICARPAGINA")]
        [Section("ESTRUCTURA")]
        public bool PublicarPagina { get; set; }

        [Description("DESCPERMISOEDITARPAGINA")]
        [Section("ESTRUCTURA")]
        public bool EditarPagina { get; set; }

        [Description("DESCPERMISOELIMINARPAGINA")]
        [Section("ESTRUCTURA")]
        public bool EliminarPagina { get; set; }

        [Description("DESCPERMISOVERCMS")]
        [Section("ESTRUCTURA")]
        public bool VerComponenteCMS { get; set; }

        [Description("DESCPERMISOCREARCMS")]
        [Section("ESTRUCTURA")]
        public bool CrearComponenteCMS { get; set; }

        [Description("DESCPERMISOEDITARCMS")]
        [Section("ESTRUCTURA")]
        public bool EditarComponenteCMS { get; set; }

        [Description("DESCPERMISOELIMINARCMS")]
        [Section("ESTRUCTURA")]
        public bool EliminarComponenteCMS { get; set; }

        [Description("DESCPERMISOMULTIMEDIACMS")]
        [Section("ESTRUCTURA")]
        public bool GestionarMultimediaCMS { get; set; }



        // GRAFO
        [Description("DESCPERMISOGESTIONAROC")]
        [Section("GRAFO")]
        public bool GestionarOC { get; set; }

        [Description("DESCPERMISOANYADIRSECUNDARIA")]
        [Section("GRAFO")]
        public bool AnyadirValorEntidadSecundaria { get; set; }

        [Description("DESCPERMISOMODIFICARSECUNDARIA")]
        [Section("GRAFO")]
        public bool ModificarValorEntidadSecundaria { get; set; }

        [Description("DESCPERMISOELIMINARSECUNDARIA")]
        [Section("GRAFO")]
        public bool EliminarValorEntidadSecundaria { get; set; }

        [Description("DESCPERMISOVERTESAURO")]
        [Section("GRAFO")]
        public bool VerTesauroSemantico { get; set; }

        [Description("DESCPERMISOANYADIRTESAURO")]
        [Section("GRAFO")]
        public bool AnyadirValorTesauro { get; set; }

        [Description("DESCPERMISOMODIFICARTESAURO")]
        [Section("GRAFO")]
        public bool ModificarValorTesauro { get; set; }

        [Description("DESCPERMISOELIMINARTESAURO")]
        [Section("GRAFO")]
        public bool EliminarValorTesauro { get; set; }

        // DESCUBRIMIENTO
        [Description("DESCPERMISOVERFACETA")]
        [Section("DESCUBRIMIENTO")]
        public bool VerFaceta { get; set; }

        [Description("DESCPERMISOCREARFACETA")]
        [Section("DESCUBRIMIENTO")]
        public bool CrearFaceta { get; set; }

        [Description("DESCPERMISOMODIFICARFACETA")]
        [Section("DESCUBRIMIENTO")]
        public bool ModificarFaceta { get; set; }

        [Description("DESCPERMISOELIMINARFACETA")]
        [Section("DESCUBRIMIENTO")]
        public bool EliminarFaceta { get; set; }


        [Description("DESCPERMISORESTAURARVERSIONCMS")]
        [Section("ESTRUCTURA")]
        public bool RestaurarVersionCMS { get; set; }

        [Description("DESCPERMISOELIMINARVERSIONCMS")]
        [Section("ESTRUCTURA")]
        public bool EliminarVersionCMS { get; set; }

        [Description("DESCPERMISORESTAURARVERSIONPAGINA")]
        [Section("ESTRUCTURA")]
        public bool RestaurarVersionPagina { get; set; }

        [Description("DESCPERMISOELIMINARVERSIONPAGINA")]
        [Section("ESTRUCTURA")]
        public bool EliminarVersionPagina { get; set; }

        /// <summary>
        /// Convierte los permisos booleanos a un valor ulong usando operaciones bit a bit
        /// </summary>
        public ulong ToUlong()
        {
            ulong permisos = 0;

            // COMUNIDAD
            if (VerCategorias) permisos |= 1;
            if (AnyadirCategoria) permisos |= 2;
            if (ModificarCategoria) permisos |= 4;
            if (EliminarCategoria) permisos |= 8;

            // ESTRUCTURA
            if (VerPagina) permisos |= 16;
            if (CrearPagina) permisos |= 32;
            if (PublicarPagina) permisos |= 64;
            if (EditarPagina) permisos |= 128;
            if (EliminarPagina) permisos |= 256;
            if (VerComponenteCMS) permisos |= 512;
            if (CrearComponenteCMS) permisos |= 1024;
            if (EditarComponenteCMS) permisos |= 2048;
            if (EliminarComponenteCMS) permisos |= 4096;
            if (GestionarMultimediaCMS) permisos |= 8192;
            if (RestaurarVersionCMS) permisos |= 67108864;
            if (EliminarVersionCMS) permisos |= 134217728;
            if (RestaurarVersionPagina) permisos |= 268435456;
            if (EliminarVersionPagina) permisos |= 536870912;

            // GRAFO
            if (GestionarOC) permisos |= 16384;
            if (AnyadirValorEntidadSecundaria) permisos |= 32768;
            if (ModificarValorEntidadSecundaria) permisos |= 65536;
            if (EliminarValorEntidadSecundaria) permisos |= 131072;
            if (VerTesauroSemantico) permisos |= 262144;
            if (AnyadirValorTesauro) permisos |= 524288;
            if (ModificarValorTesauro) permisos |= 1048576;
            if (EliminarValorTesauro) permisos |= 2097152;

            // DESCUBRIMIENTO
            if (VerFaceta) permisos |= 4194304;
            if (CrearFaceta) permisos |= 8388608;
            if (ModificarFaceta) permisos |= 16777216;
            if (EliminarFaceta) permisos |= 33554432;

            return permisos;
        }

        /// <summary>
        /// Crea una instancia de PermisosContenidosDTO desde un valor ulong
        /// </summary>
        public static PermisosContenidosDTO FromUlong(ulong permisos)
        {
            return new PermisosContenidosDTO
            {
                // COMUNIDAD
                VerCategorias = (permisos & 1) != 0,
                AnyadirCategoria = (permisos & 2) != 0,
                ModificarCategoria = (permisos & 4) != 0,
                EliminarCategoria = (permisos & 8) != 0,

                // ESTRUCTURA
                VerPagina = (permisos & 16) != 0,
                CrearPagina = (permisos & 32) != 0,
                PublicarPagina = (permisos & 64) != 0,
                EditarPagina = (permisos & 128) != 0,
                EliminarPagina = (permisos & 256) != 0,
                VerComponenteCMS = (permisos & 512) != 0,
                CrearComponenteCMS = (permisos & 1024) != 0,
                EditarComponenteCMS = (permisos & 2048) != 0,
                EliminarComponenteCMS = (permisos & 4096) != 0,
                GestionarMultimediaCMS = (permisos & 8192) != 0,
                RestaurarVersionCMS = (permisos & 67108864) != 0,
                EliminarVersionCMS = (permisos & 134217728) != 0,
                RestaurarVersionPagina = (permisos & 268435456) != 0,
                EliminarVersionPagina = (permisos & 536870912) != 0,

                // GRAFO
                GestionarOC = (permisos & 16384) != 0,
                AnyadirValorEntidadSecundaria = (permisos & 32768) != 0,
                ModificarValorEntidadSecundaria = (permisos & 65536) != 0,
                EliminarValorEntidadSecundaria = (permisos & 131072) != 0,
                VerTesauroSemantico = (permisos & 262144) != 0,
                AnyadirValorTesauro = (permisos & 524288) != 0,
                ModificarValorTesauro = (permisos & 1048576) != 0,
                EliminarValorTesauro = (permisos & 2097152) != 0,

                // DESCUBRIMIENTO
                VerFaceta = (permisos & 4194304) != 0,
                CrearFaceta = (permisos & 8388608) != 0,
                ModificarFaceta = (permisos & 16777216) != 0,
                EliminarFaceta = (permisos & 33554432) != 0
            };
        }




    }


    //PERMISOS ECOSISTEMA
    public class PermisosEcosistemaDTO
    {
        // ECOSISTEMA
        [Description("DESCPERMISOECOSISTEMATRADUCCIONES")]
        [Section("ECOSISTEMA")]
        public bool GestionarTraduccionesEcosistema { get; set; }

        [Description("DESCPERMISOECOSISTEMADATOSEXTRA")]
        [Section("ECOSISTEMA")]
        public bool GestionarDatosExtraRegistroEcosistema { get; set; }

        [Description("DESCPERMISOECOSISTEMACORREO")]
        [Section("ECOSISTEMA")]
        public bool GestionarBuzonDeCorreoEcosistema { get; set; }

        [Description("DESCPERMISOECOSISTEMAEVENTOS")]
        [Section("ECOSISTEMA")]
        public bool GestionarEventosExternosEcosistema { get; set; }

        [Description("DESCPERMISOECOSISTEMACATEGORIAS")]
        [Section("ECOSISTEMA")]
        public bool GestionarCategoriasDePlataforma { get; set; }

        [Description("DESCPERMISOECOSISTEMACONFIGURACION")]
        [Section("ECOSISTEMA")]
        public bool GestionarLaConfiguracionPlataforma { get; set; }

        [Description("DESCPERMISOECOSISTEMASHAREPOINT")]
        [Section("ECOSISTEMA")]
        public bool ConfiguracionDeSharePoint { get; set; }

        [Description("DESCPERMISOECOSISTEMAVISTAS")]
        [Section("ECOSISTEMA")]
        public bool GestionarVistasEcosistema { get; set; }

        [Description("DESCPERMISOECOSISTEMAIC")]
        [Section("ECOSISTEMA")]
        public bool AdministrarIntegracionContinua { get; set; }

        [Description("DESCPERMISOECOSISTEMASOLICITUDES")]
        [Section("ECOSISTEMA")]
        public bool AdministrarSolicitudesComunidad { get; set; }

        [Description("DESCPERMISOECOSISTEMAROLES")]
        [Section("ECOSISTEMA")]
        public bool GestionarRolesYPermisosEcosistema { get; set; }

        [Description("DESCPERMISOECOSISTEMAMIEMBROS")]
        [Section("ECOSISTEMA")]
        public bool AdministrarMiembrosEcosistema { get; set; }

        /// <summary>
        /// Convierte los permisos booleanos a un valor ulong usando operaciones bit a bit
        /// </summary>
        public ulong ToUlong()
        {
            ulong permisos = 0;

            if (GestionarTraduccionesEcosistema) permisos |= 1;
            if (GestionarDatosExtraRegistroEcosistema) permisos |= 2;
            if (GestionarBuzonDeCorreoEcosistema) permisos |= 4;
            if (GestionarEventosExternosEcosistema) permisos |= 8;
            if (GestionarCategoriasDePlataforma) permisos |= 16;
            if (GestionarLaConfiguracionPlataforma) permisos |= 32;
            if (ConfiguracionDeSharePoint) permisos |= 64;
            if (GestionarVistasEcosistema) permisos |= 128;
            if (AdministrarIntegracionContinua) permisos |= 256;
            if (AdministrarSolicitudesComunidad) permisos |= 512;
            if (GestionarRolesYPermisosEcosistema) permisos |= 1024;
            if (AdministrarMiembrosEcosistema) permisos |= 2048;

            return permisos;
        }

        /// <summary>
        /// Crea una instancia de PermisosEcosistemaDTO desde un valor ulong
        /// </summary>
        public static PermisosEcosistemaDTO FromUlong(ulong permisos)
        {
            return new PermisosEcosistemaDTO
            {
                GestionarTraduccionesEcosistema = (permisos & 1) != 0,
                GestionarDatosExtraRegistroEcosistema = (permisos & 2) != 0,
                GestionarBuzonDeCorreoEcosistema = (permisos & 4) != 0,
                GestionarEventosExternosEcosistema = (permisos & 8) != 0,
                GestionarCategoriasDePlataforma = (permisos & 16) != 0,
                GestionarLaConfiguracionPlataforma = (permisos & 32) != 0,
                ConfiguracionDeSharePoint = (permisos & 64) != 0,
                GestionarVistasEcosistema = (permisos & 128) != 0,
                AdministrarIntegracionContinua = (permisos & 256) != 0,
                AdministrarSolicitudesComunidad = (permisos & 512) != 0,
                GestionarRolesYPermisosEcosistema = (permisos & 1024) != 0,
                AdministrarMiembrosEcosistema = (permisos & 2048) != 0
            };
        }


    }

    //PERMISOS RECURSOS
    public class PermisosRecursosDTO
    {
        // RECURSOS - Adjuntos
        [Description("DESCPERMISOCREARADJUNTO")]
        [Section("RECURSOS")]
        public bool CrearRecursoTipoAdjunto { get; set; }

        [Description("DESCPERMISOEDITARADJUNTO")]
        [Section("RECURSOS")]
        public bool EditarRecursoTipoAdjunto { get; set; }

        [Description("DESCPERMISOELIMINARADJUNTO")]
        [Section("RECURSOS")]
        public bool EliminarRecursoTipoAdjunto { get; set; }

        // RECURSOS - Referencias
        [Description("DESCPERMISOCREARREFERENCIA")]
        [Section("RECURSOS")]
        public bool CrearRecursoTipoReferenciaADocumentoFisico { get; set; }

        [Description("DESCPERMISOEDITARREFERENCIA")]
        [Section("RECURSOS")]
        public bool EditarRecursoTipoReferenciaADocumentoFisico { get; set; }

        [Description("DESCPERMISOELIMINARREFERNCIA")]
        [Section("RECURSOS")]
        public bool EliminarRecursoTipoReferenciaADocumentoFisico { get; set; }

        // RECURSOS - Enlaces
        [Description("DESCPERMISOCREARENLACE")]
        [Section("RECURSOS")]
        public bool CrearRecursoTipoEnlace { get; set; }

        [Description("DESCPERMISOEDITARENLACE")]
        [Section("RECURSOS")]
        public bool EditarRecursoTipoEnlace { get; set; }

        [Description("DESCPERMISOELIMINARENLACE")]
        [Section("RECURSOS")]
        public bool EliminarRecursoTipoEnlace { get; set; }

        // RECURSOS - Notas
        [Description("DESCPERMISOCREARNOTA")]
        [Section("RECURSOS")]
        public bool CrearNota { get; set; }

        [Description("DESCPERMISOEDITARNOTA")]
        [Section("RECURSOS")]
        public bool EditarNota { get; set; }

        [Description("DESCPERMISOELIMINARNOTA")]
        [Section("RECURSOS")]
        public bool EliminarNota { get; set; }

        // RECURSOS - Preguntas
        [Description("DESCPERMISOCREARPREGUNTA")]
        [Section("RECURSOS")]
        public bool CrearPregunta { get; set; }

        [Description("DESCPERMISOEDITARPREGUNTA")]
        [Section("RECURSOS")]
        public bool EditarPregunta { get; set; }

        [Description("DESCPERMISOELIMINARPREGUNTA")]
        [Section("RECURSOS")]
        public bool EliminarPregunta { get; set; }

        // RECURSOS - Encuestas
        [Description("DESCPERMISOCREARENCUESTA")]
        [Section("RECURSOS")]
        public bool CrearEncuesta { get; set; }

        [Description("DESCPERMISOEDITARENCUESTA")]
        [Section("RECURSOS")]
        public bool EditarEncuesta { get; set; }

        [Description("DESCPERMISOELIMINARENCUESTA")]
        [Section("RECURSOS")]
        public bool EliminarEncuesta { get; set; }

        // RECURSOS - Debates
        [Description("DESCPERMISOCREARDEBATE")]
        [Section("RECURSOS")]
        public bool CrearDebate { get; set; }

        [Description("DESCPERMISOEDITARDEBATE")]
        [Section("RECURSOS")]
        public bool EditarDebate { get; set; }

        [Description("DESCPERMISOELIMINARDEBATE")]
        [Section("RECURSOS")]
        public bool EliminarDebate { get; set; }

        // RECURSOS - Semánticos
        [Description("DESCPERMISOCREARSEMANTICO")]
        [Section("RECURSOS")]
        public bool CrearRecursoSemantico { get; set; }

        [Description("DESCPERMISOEDITARSEMANTICO")]
        [Section("RECURSOS")]
        public bool EditarRecursoSemantico { get; set; }

        [Description("DESCPERMISOELIMINARSEMANTICO")]
        [Section("RECURSOS")]
        public bool EliminarRecursoSemantico { get; set; }

        // RECURSOS - Versiones Enlaces
        [Description("DESCPERMISORESTAURARVERSIONENLACE")]
        [Section("RECURSOS")]
        public bool RestaurarVersionEnlace { get; set; }

        [Description("DESCPERMISOELIMINARVERSIONENLACE")]
        [Section("RECURSOS")]
        public bool EliminarVersionEnlace { get; set; }

        // RECURSOS - Versiones Adjuntos
        [Description("DESCPERMISORESTAURARVERSIONADJUNTO")]
        [Section("RECURSOS")]
        public bool RestaurarVersionAdjunto { get; set; }

        [Description("DESCPERMISOELIMINARVERSIONADJUNTO")]
        [Section("RECURSOS")]
        public bool EliminarVersionAdjunto { get; set; }

        // RECURSOS - Versiones Referencias
        [Description("DESCPERMISORESTAURARVERSIONREFERNCIA")]
        [Section("RECURSOS")]
        public bool RestaurarVersionReferencia { get; set; }

        [Description("DESCPERMISOELIMINARVERSIONREFERNCIA")]
        [Section("RECURSOS")]
        public bool EliminarVersionReferencia { get; set; }

        // RECURSOS - Versiones Notas
        [Description("DESCPERMISORESTAURARVERSIONNOTA")]
        [Section("RECURSOS")]
        public bool RestaurarVersionNota { get; set; }

        [Description("DESCPERMISORESELIMINARVERSIONNOTA")]
        [Section("RECURSOS")]
        public bool EliminarVersionNota { get; set; }

        // RECURSOS - Versiones Preguntas
        [Description("DESCPERMISORESTAURARVERSIONPREGUNTA")]
        [Section("RECURSOS")]
        public bool RestaurarVersionPregunta { get; set; }

        [Description("DESCPERMISOELIMINARVERSIONPREGUNTA")]
        [Section("RECURSOS")]
        public bool EliminarVersionPregunta { get; set; }

        // RECURSOS - Versiones Encuestas
        [Description("DESCPERMISORESTAURARVERSIONENCUESTA")]
        [Section("RECURSOS")]
        public bool RestaurarVersionEncuesta { get; set; }

        [Description("DESCPERMISOELIMINARVERSIONENCUESTA")]
        [Section("RECURSOS")]
        public bool EliminarVersionEncuesta { get; set; }

        // RECURSOS - Versiones Debates
        [Description("DESCPERMISORESTAURARVERSIONDEBATE")]
        [Section("RECURSOS")]
        public bool RestaurarVersionDebate { get; set; }

        [Description("DESCPERMISOELIMINARVERSIONDEBATE")]
        [Section("RECURSOS")]
        public bool EliminarVersionDebate { get; set; }

        // RECURSOS - Certificación
        [Description("DESCPERMISOCERTIFICARRECURSO")]
        [Section("RECURSOS")]
        public bool CertificarRecurso { get; set; }

        /// <summary>
        /// Convierte los permisos booleanos a un valor ulong usando operaciones bit a bit
        /// </summary>
        public ulong ToUlong()
        {
            ulong permisos = 0;

            // Adjuntos
            if (CrearRecursoTipoAdjunto) permisos |= 1;
            if (EditarRecursoTipoAdjunto) permisos |= 2;
            if (EliminarRecursoTipoAdjunto) permisos |= 4;

            // Referencias
            if (CrearRecursoTipoReferenciaADocumentoFisico) permisos |= 8;
            if (EditarRecursoTipoReferenciaADocumentoFisico) permisos |= 16;
            if (EliminarRecursoTipoReferenciaADocumentoFisico) permisos |= 32;

            // Enlaces
            if (CrearRecursoTipoEnlace) permisos |= 64;
            if (EditarRecursoTipoEnlace) permisos |= 128;
            if (EliminarRecursoTipoEnlace) permisos |= 256;

            // Notas
            if (CrearNota) permisos |= 512;
            if (EditarNota) permisos |= 1024;
            if (EliminarNota) permisos |= 2048;

            // Preguntas
            if (CrearPregunta) permisos |= 4096;
            if (EditarPregunta) permisos |= 8192;
            if (EliminarPregunta) permisos |= 16384;

            // Encuestas
            if (CrearEncuesta) permisos |= 32768;
            if (EditarEncuesta) permisos |= 65536;
            if (EliminarEncuesta) permisos |= 131072;

            // Debates
            if (CrearDebate) permisos |= 262144;
            if (EditarDebate) permisos |= 524288;
            if (EliminarDebate) permisos |= 1048576;

            // Semánticos
            if (CrearRecursoSemantico) permisos |= 2097152;
            if (EditarRecursoSemantico) permisos |= 4194304;
            if (EliminarRecursoSemantico) permisos |= 8388608;

            // Versiones Enlaces
            if (RestaurarVersionEnlace) permisos |= 16777216;
            if (EliminarVersionEnlace) permisos |= 33554432;

            // Versiones Adjuntos
            if (RestaurarVersionAdjunto) permisos |= 67108864;
            if (EliminarVersionAdjunto) permisos |= 134217728;

            // Versiones Referencias
            if (RestaurarVersionReferencia) permisos |= 268435456;
            if (EliminarVersionReferencia) permisos |= 536870912;

            // Versiones Notas
            if (RestaurarVersionNota) permisos |= 1073741824;
            if (EliminarVersionNota) permisos |= 2147483648;

            // Versiones Preguntas
            if (RestaurarVersionPregunta) permisos |= 4294967296;
            if (EliminarVersionPregunta) permisos |= 8589934592;

            // Versiones Encuestas
            if (RestaurarVersionEncuesta) permisos |= 17179869184;
            if (EliminarVersionEncuesta) permisos |= 34359738368;

            // Versiones Debates
            if (RestaurarVersionDebate) permisos |= 68719476736;
            if (EliminarVersionDebate) permisos |= 137438953472;

            // Certificación
            if (CertificarRecurso) permisos |= 274877906944;

            return permisos;
        }

        /// <summary>
        /// Crea una instancia de PermisosRecursosDTO desde un valor ulong
        /// </summary>
        public static PermisosRecursosDTO FromUlong(ulong permisos)
        {
            return new PermisosRecursosDTO
            {
                // Adjuntos
                CrearRecursoTipoAdjunto = (permisos & 1) != 0,
                EditarRecursoTipoAdjunto = (permisos & 2) != 0,
                EliminarRecursoTipoAdjunto = (permisos & 4) != 0,

                // Referencias
                CrearRecursoTipoReferenciaADocumentoFisico = (permisos & 8) != 0,
                EditarRecursoTipoReferenciaADocumentoFisico = (permisos & 16) != 0,
                EliminarRecursoTipoReferenciaADocumentoFisico = (permisos & 32) != 0,

                // Enlaces
                CrearRecursoTipoEnlace = (permisos & 64) != 0,
                EditarRecursoTipoEnlace = (permisos & 128) != 0,
                EliminarRecursoTipoEnlace = (permisos & 256) != 0,

                // Notas
                CrearNota = (permisos & 512) != 0,
                EditarNota = (permisos & 1024) != 0,
                EliminarNota = (permisos & 2048) != 0,

                // Preguntas
                CrearPregunta = (permisos & 4096) != 0,
                EditarPregunta = (permisos & 8192) != 0,
                EliminarPregunta = (permisos & 16384) != 0,

                // Encuestas
                CrearEncuesta = (permisos & 32768) != 0,
                EditarEncuesta = (permisos & 65536) != 0,
                EliminarEncuesta = (permisos & 131072) != 0,

                // Debates
                CrearDebate = (permisos & 262144) != 0,
                EditarDebate = (permisos & 524288) != 0,
                EliminarDebate = (permisos & 1048576) != 0,

                // Semánticos
                CrearRecursoSemantico = (permisos & 2097152) != 0,
                EditarRecursoSemantico = (permisos & 4194304) != 0,
                EliminarRecursoSemantico = (permisos & 8388608) != 0,

                // Versiones Enlaces
                RestaurarVersionEnlace = (permisos & 16777216) != 0,
                EliminarVersionEnlace = (permisos & 33554432) != 0,

                // Versiones Adjuntos
                RestaurarVersionAdjunto = (permisos & 67108864) != 0,
                EliminarVersionAdjunto = (permisos & 134217728) != 0,

                // Versiones Referencias
                RestaurarVersionReferencia = (permisos & 268435456) != 0,
                EliminarVersionReferencia = (permisos & 536870912) != 0,

                // Versiones Notas
                RestaurarVersionNota = (permisos & 1073741824) != 0,
                EliminarVersionNota = (permisos & 2147483648) != 0,

                // Versiones Preguntas
                RestaurarVersionPregunta = (permisos & 4294967296) != 0,
                EliminarVersionPregunta = (permisos & 8589934592) != 0,

                // Versiones Encuestas
                RestaurarVersionEncuesta = (permisos & 17179869184) != 0,
                EliminarVersionEncuesta = (permisos & 34359738368) != 0,

                // Versiones Debates
                RestaurarVersionDebate = (permisos & 68719476736) != 0,
                EliminarVersionDebate = (permisos & 137438953472) != 0,

                // Certificación
                CertificarRecurso = (permisos & 274877906944) != 0
            };
        }

    }
}