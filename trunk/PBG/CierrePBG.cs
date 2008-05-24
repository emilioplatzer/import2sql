/*
 * Creado por SharpDevelop.
 * Usuario: Administrador
 * Fecha: 21/05/2008
 * Hora: 05:46 p.m.
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using NUnit.Framework;

using Comunes;
using DelOffice;

namespace PBG
{
	public delegate void AccionExplicada(ref string explicacion);
	public class CierrePBG{
		public ParametrosCierrePBG param;
		public const string NombreHojaProcesamiento="Procesamiento";
		public const string NombreHojaTablaClanae="Clanae";
		public const string NombreHojaBitacora="Bitacora";
		public const string NombreHojaResultados="Resultados";
		public LibroExcel libro;
		HojaExcel hojaProcesamiento;
		HojaExcel hojaBitacora;
		HojaExcel hojaResultados;
		public CierrePBG(ParametrosCierrePBG param){
			this.param=param;
			TratarVarios(delegate(ref string haciendo){
				haciendo="abrir el libro excel "+param.ExcelComandante;
				libro=LibroExcel.Abrir(param.ExcelComandante);
				string en=" en el libro excel "+param.ExcelComandante;
				haciendo="acceder a la hoja "+NombreHojaProcesamiento+en;
				hojaProcesamiento=libro.Hoja(NombreHojaProcesamiento);
				haciendo="acceder a la hoja "+NombreHojaBitacora+en;
				try{
					hojaBitacora=libro.Hoja(NombreHojaBitacora);
				}catch(System.Runtime.InteropServices.COMException ex){
					haciendo="crear la hoja "+NombreHojaBitacora+en;
					hojaBitacora=libro.NuevaHoja(NombreHojaBitacora);
				}
				string[,] columnasBitacora={{"Carpeta","Archivo","Responsable","Fecha Archivo","Fecha Procesamiento","Ramas Encontradas","Error","Mensaje del sistema"}};
				hojaBitacora.Rellenar(columnasBitacora);
				haciendo="acceder a la hoja "+NombreHojaResultados+en;
				try{
					hojaResultados=libro.Hoja(NombreHojaResultados);
				}catch(System.Runtime.InteropServices.COMException ex){
					haciendo="crear la hoja "+NombreHojaResultados+en;
					hojaResultados=libro.NuevaHoja(NombreHojaResultados);
				}
				string[,] columnasResultado={{"Año","Digito","Clanae 2 Digitos","Clanae 3 Digitos","Clanae 5 Digitos","DESCRIP","Sectorialista","-","Vbp_c","va_c","Ci_c","Vbp_k","va_k","Ci_k"}};
				hojaResultados.Rellenar(columnasResultado);
				haciendo="guardar los datos estructurales "+en;
				libro.GuardarYCerrar();
			});
		}
		public void TratarVarios(AccionExplicada accion){
			string haciendo="";
		reintentar:
			try{
				accion(ref haciendo);
			}catch(Exception ex){
				string atencion="Atencion";
			repetirmensaje:
				System.Windows.Forms.DialogResult opcion=
					System.Windows.Forms.MessageBox.Show(
						"Error al "+haciendo+" ERROR INFORMADO: "+ex.Message
						,atencion
						,System.Windows.Forms.MessageBoxButtons.AbortRetryIgnore
						,System.Windows.Forms.MessageBoxIcon.Hand
						,System.Windows.Forms.MessageBoxDefaultButton.Button3);
				if(opcion==System.Windows.Forms.DialogResult.Retry){
					goto reintentar;
				}
				if(opcion==System.Windows.Forms.DialogResult.Ignore){
					atencion="No se puede ignoarar";
					goto repetirmensaje;
				}
				if(opcion==System.Windows.Forms.DialogResult.Abort){
					throw;
				}
			}
		}
		public void Procesar(){
			
		}
	}
	public class ParametrosCierrePBG:Parametros{
		public string ExcelComandante;
		public ParametrosCierrePBG(string NombreAplicacion)
			:base(LeerPorDefecto.SI,NombreAplicacion)
		{}
		public ParametrosCierrePBG()
			:base(LeerPorDefecto.NO)
		{}
	}
	[TestFixture]
	public class PrCierrePBG{
		ParametrosCierrePBG param;
		CierrePBG cierre;
		LibroExcel libroCierre;
		public PrCierrePBG(){
			param=new ParametrosCierrePBG();
			param.ExcelComandante=Archivo.CarpetaActual()+@"\CierrePBG_prueba";
			Archivo.Borrar(param.ExcelComandante);
			libroCierre=LibroExcel.Nuevo();
			CrearTablaClanae();
			CrearTablaProcesamiento();
			CrearEjemploAgricultura();
			CrearEjemploProdAli();
			libroCierre.GuardarYCerrar(param.ExcelComandante);
			cierre=new CierrePBG(param);
		}
		~PrCierrePBG(){
			libroCierre.GuardarYCerrar();
		}
		public void CrearTablaClanae(){
			HojaExcel HojaTablaClanae=libroCierre.NuevaHoja(CierrePBG.NombreHojaTablaClanae);
			string[,] TablaClanaeEjemplo={
				{"Clanae 5 digitos","descripcion"},
				{"01110","Cultivo de cereales, oleaginosas y forrajeras"},
				{"01120","Cultivo de hortalizas, legumbres, flores y plantas ornamentales"},
				{"15000","ELABORACION DE PRODUCTOS ALIMENTICIOS Y BEBIDAS"}
			};
			RangoExcel rango=HojaTablaClanae.Rango(1,3,5000,4);
			rango.Rellenar(TablaClanaeEjemplo);
			HojaTablaClanae.PonerTexto(1,1,"Clanae 2 digitos");
			HojaTablaClanae.PonerTexto(1,2,"Clanae 3 digitos");
			for(int fila=2; fila<=TablaClanaeEjemplo.GetLength(0); fila++){
				HojaTablaClanae.PonerTexto(fila,1,TablaClanaeEjemplo[fila-1,0].Substring(0,2));
				HojaTablaClanae.PonerTexto(fila,2,TablaClanaeEjemplo[fila-1,0].Substring(0,3));
			}
		}
		public void CrearTablaProcesamiento(){
			HojaExcel Hoja=libroCierre.NuevaHoja(CierrePBG.NombreHojaProcesamiento);
			string[,] TablaProcesamiento={
				{"Carpeta","Archivo","Responsable"},
				{"esta","Agricultura.xls","José"},
				{"esta","Industria.xls","José"},
				{@"c:\noExiste","Minería.xls","José"},
				{Archivo.CarpetaActual()+@"\temp_borrar","ProdAlim","María"}
			};
			Hoja.Rellenar(TablaProcesamiento);
		}
		public void CrearEjemplo(object[,] datos,string NombreArchivo,string HojaNombre){
			Archivo.Borrar(NombreArchivo);
			LibroExcel libro=LibroExcel.Nuevo();
			HojaExcel hoja=libro.NuevaHoja(HojaNombre);
			hoja.Rellenar(datos);
			libro.GuardarYCerrar(NombreArchivo);
		}
		public void CrearEjemploAgricultura(){
			object[,] datos={
				{"Tabla!RA!","Sector",15000,"Desde",1993,null,null},
				{"basura",0,0,0,0,0,0},
				{null,0,0,0,0,0,0},
				{"Años","VBP a corrientes","VA a precios corrientes","Consumo Intermedio a precios corrientes","VBP a precios de 1993","VA  a precios de 1993","Consumo Intermedio a precios de 1993"},
				{1993,21800,11000,10800,21800,11000,10800},
				{1994,22800,12000,10800,23800,12000,11800},
				{1995,23800,13000,10800,25800,13000,12800},
				{null,0,0,0,0,0,0},
				{null,0,0,0,0,0,0},
				{null,0,0,0,0,0,0},
				{"Tabla!RA!","Sector","01120","Desde",1993,null,null},
				{"titulos",0,0,0,0,0,0},
				{null,0,0,0,0,0,0},
				{null,0,0,0,0,0,0},
				{"Años","VBP a precios corrientes","VA a precios corrientes","Consumo Intermedio a precios corrientes","VBP a precios de 1993","VA  a precios de 1993","Consumo Intermedio a precios de 1993"},
				{1993,1800,1000,800,1800,1000,800},
				{1994,2800,2000,800,3800,2000,1800},
				{1995,3800,3000,800,5800,3000,2800}
			};
			CrearEjemplo(datos,Archivo.CarpetaActual()+@"\Agricultura.xls","Agri");
		}
		public void CrearEjemploProdAli(){
			object[,] datos={
				{"Tabla!RA!","Sector",01110,"Desde",1993,null,null},
				{null,0,0,0,0,0,0},
				{null,0,0,0,0,0,0},
				{null,0,0,0,0,0,1993},
				{"Años","VBP a precios corrientes","VA a precios corrientes","Consumo Intermedio a precios corrientes","VBP a precios de 1993","VA  a precios de 1993","Consumo Intermedio a precios de 1993"},
				{1993,18763.94047,10220.71837,8543.222095,18763.94047,10220.71837,8543.222095},
				{1994,19837.6915,10805.590,9032.1009,19042.1474,10372.25769,8669.889713},
				{1995,20813.45171,11337.0871,9476.3645,19326.39177,10527.0856,8799.306174},
				{null,0,0,0,0,0,0},
				{null,0,0,0,0,0,0},
				{"Tabla!RA!","Sector","91120","Desde",1993,null,null},
				{"basura",0,0,0,0,0,0},
				{null,0,0,0,0,0,0},
				{"Años","VBP a precios corrientes","VA a precios corrientes","Consumo Intermedio a precios corrientes","VBP a precios de 1993","VA  a precios de 1993","Consumo Intermedio a precios de 1993"},
				{1993,1800,1000,800,1800,1000,800},
				{1994,2800,2000,800,3800,2000,1800},
				{1995,3800,3000,800,5800,3000,2800}
			};
			CrearEjemplo(datos,Archivo.CarpetaActual()+@"\temp_borrar\ProdAli.xls","ProdAlim");
		}
		[Test]
		public void Estamos(){
			cierre.Procesar();
			HojaExcel hResultados=cierre.libro.Hoja(CierrePBG.NombreHojaResultados);
			Assert.AreEqual("Año",hResultados.ValorCelda("A1"));
			Assert.AreEqual(1993,hResultados.ValorCelda("A2"));
			Assert.AreEqual("01110",hResultados.ValorCelda("E2"));
			HojaExcel hBitacora=cierre.libro.Hoja(CierrePBG.NombreHojaBitacora);
			Assert.AreEqual("Agricultura.xls",hBitacora.ValorCelda("B2"));
		}
	}
}
