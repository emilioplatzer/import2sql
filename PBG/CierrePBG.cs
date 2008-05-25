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
		public const string NombreHojaResumen="Resumen";
		public const string NombreHojaResultados="Resultados";
		public const int colResumenCarpetayArchivo=1;
		public const int colResumen=1;
		public LibroExcel libro;
		HojaExcel hojaProcesamiento;
		HojaExcel hojaResumen;
		HojaExcel hojaResultados;
		Resumen resumen;
		string haciendo;
		int lineaResultados;
		class Resumen{
			public string Carpeta_y_Archivo;
			public string Responsable;
			public DateTime Fecha_Archivo;
			public DateTime Fecha_Procesamiento;
			public Lista<string> Ramas_Encontradas=new Lista<string>();
			public string Error;
			public string Mensaje_del_sistema;
		}
		public CierrePBG(ParametrosCierrePBG param){
			this.param=param;
			TratarVarios(delegate(ref string haciendo){
				haciendo="abriendo el libro excel "+param.ExcelComandante;
				libro=LibroExcel.Abrir(param.ExcelComandante);
				string en=" en el libro excel "+param.ExcelComandante;
				haciendo="accediendo a la hoja "+NombreHojaProcesamiento+en;
				hojaProcesamiento=libro.Hoja(NombreHojaProcesamiento);
				haciendo="accediendo a la hoja "+NombreHojaResumen+en;
				try{
					hojaResumen=libro.Hoja(NombreHojaResumen);
				}catch(System.Runtime.InteropServices.COMException ex){
					haciendo="creando la hoja "+NombreHojaResumen+en;
					hojaResumen=libro.NuevaHoja(NombreHojaResumen);
				}
				hojaResumen.RellenarHorizontal(new Resumen().NombresMiembros());
				haciendo="accediendo a la hoja "+NombreHojaResultados+en;
				try{
					hojaResultados=libro.Hoja(NombreHojaResultados);
				}catch(System.Runtime.InteropServices.COMException ex){
					haciendo="creando la hoja "+NombreHojaResultados+en;
					hojaResultados=libro.NuevaHoja(NombreHojaResultados);
					lineaResultados=2;
				}
				string[,] columnasResultado={{"Año","Digito","Clanae 2 Digitos","Clanae 3 Digitos","Clanae 5 Digitos","DESCRIP","Sectorialista","-","Vbp_c","va_c","Ci_c","Vbp_k","va_k","Ci_k"}};
				hojaResultados.Rellenar(columnasResultado);
				haciendo="guardando los datos estructurales "+en;
				libro.Guardar();
			});
		}
		~CierrePBG(){
			libro.GuardarYCerrar();
		}
		public void TratarVarios(AccionExplicada accion){
			haciendo="";
		reintentar:
			try{
				accion(ref haciendo);
			}catch(Exception ex){
				string atencion="Atencion";
			repetirmensaje:
				System.Windows.Forms.DialogResult opcion=
					System.Windows.Forms.MessageBox.Show(
						"Error "+haciendo+" ERROR INFORMADO: "+ex.Message
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
		public bool QueDiga(RangoExcel rango, string Leyenda, int columna){
			if(Leyenda!=rango.TextoCelda(1,columna)){
			   	resumen.Error="Error controlando que diga "+Leyenda+". Dice '"+rango.TextoCelda(1,columna)+"' en la hoja "+rango.NombreHoja;
			   	return false;
			}
			return true;
		}
		public void ProcesarTabla(RangoExcel rango){
			if(QueDiga(rango,"Rama",2) 
			   && QueDiga(rango,"Desde",4))
			{
				haciendo="obteniendo la rama";
				string rama=rango.TextoCelda(1,3);
				resumen.Ramas_Encontradas.Add(rama);
				haciendo="buscando la indicación de cuál es el primer año a considerar. Dice '"+rango.TextoCelda(1,5)+"'";
				int desdeAno=rango.NumeroCelda(1,5);
				int ano=0;
				int fila=1;
				while(ano!=desdeAno && fila<22){
					fila++;
					try{
						ano=rango.NumeroCelda(fila,1);
					}catch(InvalidCastException){
					}
				}
				if(ano!=desdeAno){
					resumen.Error="Error no se encontró el primer renglón con año "+desdeAno+" en hoja "+rango.NombreHoja;
				}else{
					while(ano>0){
						haciendo="pasando la fila "+fila+" de la hoja "+rango.NombreHoja;
						hojaResultados.PonerValor(lineaResultados,1,ano);
						hojaResultados.PonerValor(lineaResultados,2,5);
						hojaResultados.PonerTexto(lineaResultados,3,rama.Substring(0,2));
						hojaResultados.PonerTexto(lineaResultados,4,rama.Substring(0,3));
						hojaResultados.PonerTexto(lineaResultados,5,rama);
						for(int col=2; col<9; col++){
							object valor=rango.ValorCelda(fila,col);
							hojaResultados.PonerValor(lineaResultados,col+7,valor);
						}
						lineaResultados++;
						fila++;
						ano=rango.NumeroCelda(fila,1);
					}
				}
				libro.Guardar();
			}
		}
		public void ProcesarLibro(string Carpeta,string archivo,int linea){
			resumen=new Resumen();
			string NombreArchivo=
				(Carpeta==null || Carpeta=="esta"?Archivo.CarpetaActual():Carpeta)
				+@"\"+archivo
				+(archivo.ToLower().EndsWith(".xls")?"":".xls");
			resumen.Carpeta_y_Archivo=NombreArchivo;
			resumen.Fecha_Procesamiento=DateTime.Now;
			hojaResumen.PonerHorizontal(linea,1,resumen);
			libro.Guardar();
			haciendo="";
			try{
				haciendo="buscando el archivo "+NombreArchivo;
				resumen.Carpeta_y_Archivo=System.IO.Path.GetFullPath(NombreArchivo);
				resumen.Fecha_Archivo=System.IO.File.GetCreationTime(NombreArchivo);
				haciendo="abriendo el libro de excel "+NombreArchivo;
				LibroExcel libroDatos=LibroExcel.Abrir(NombreArchivo);
				haciendo="buscando entre las hojas una Tabla!RA!";
				Conjunto<RangoExcel> Rangos=new Conjunto<RangoExcel>();
			    for(int i=1; i<=libroDatos.CantidadHojas; i++){
					HojaExcel hoja=libroDatos.Hoja(i);
					RangoExcel rango=hoja.BuscarPorColumnas("Tabla!RA!");
					Conjunto<int> Vistos=new Conjunto<int>();
					while(rango.EsValido && !Vistos.Contiene(rango.NumeroFila*256+rango.NumeroColumna)){
				      	Vistos.Add(rango.NumeroFila*256+rango.NumeroColumna);
						Console.WriteLine("voy por {0} {1},{2} ",rango.NombreHoja,rango.NumeroFila,rango.NumeroColumna);
						rango=hoja.BuscarProximo();
						if(rango.EsValido){
							Rangos.Add(rango);
						}
					}
				}
				haciendo="recorriendo los rangos donde hay tablas";
				foreach(RangoExcel r in Rangos.Keys){
					ProcesarTabla(r);
				}
				libroDatos.CerrarNoHayCambios();
			}catch(Exception ex){
				resumen.Error="Error "+haciendo;
				resumen.Mensaje_del_sistema=ex.Message+":"+ex.GetType().FullName;
			}
			hojaResumen.PonerHorizontal(linea,1,resumen);
		}
		public void ProcesarLista(){
			string Carpeta;
			string Archivo;
			int linea=2;
			while(true){
				Carpeta=hojaProcesamiento.TextoCelda(linea,1);
				Archivo=hojaProcesamiento.TextoCelda(linea,2);
				if(Archivo==null || Archivo.Trim()==""){
					break;
				}
				ProcesarLibro(Carpeta,Archivo,linea);
				linea++;
			}
			libro.Guardar();
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
				{null,null,null,null,null,null,null},
				{"Tabla!RA!","Rama",15000,"Desde",1993,null,null},
				{"basura",0,0,0,0,0,0},
				{null,null,null,null,null,null,null},
				{"Años","VBP a corrientes","VA a precios corrientes","Consumo Intermedio a precios corrientes","VBP a precios de 1993","VA  a precios de 1993","Consumo Intermedio a precios de 1993"},
				{1993,21800,11000,10800,21800,11000,10800},
				{1994,22800,12000,10800,23800,12000,11800},
				{1995,23800,13000,10800,25800,13000,12800},
				{null,0,0,0,0,0,0},
				{null,null,null,null,null,null,null},
				{null,null,null,null,null,null,null},
				{null,null,null,null,null,null,null},
				{"Tabla!RA!","Rama","01120","Desde",1993,null,null},
				{"titulos",0,0,0,0,0,0},
				{null,null,null,null,null,null,null},
				{null,null,null,null,null,null,null},
				{null,null,null,null,null,null,null},
				{"Años","VBP a precios corrientes","VA a precios corrientes","Consumo Intermedio a precios corrientes","VBP a precios de 1993","VA  a precios de 1993","Consumo Intermedio a precios de 1993"},
				{1993,1800,1000,800,1800,1000,800},
				{1994,2800,2000,800,3800,2000,1800},
				{1995,3800,3000,800,5800,3000,2800},
				{null,null,null,null,null,null,null},
				{null,null,null,null,null,null,null}
			};
			CrearEjemplo(datos,Archivo.CarpetaActual()+@"\Agricultura.xls","Agri");
		}
		public void CrearEjemploProdAli(){
			object[,] datos={
				{null,null,null,null,null,null,null,null},
				{null,"Tabla!RA!","Rama",01110,"Desde",1993,null,null},
				{null,null,0,0,0,0,0,0},
				{null,null,0,0,0,0,0,0},
				{null,null,0,0,0,0,0,1993},
				{null,"Años","VBP a precios corrientes","VA a precios corrientes","Consumo Intermedio a precios corrientes","VBP a precios de 1993","VA  a precios de 1993","Consumo Intermedio a precios de 1993"},
				{null,1993,18763.94047,10220.71837,8543.222095,18763.94047,10220.71837,8543.222095},
				{null,1994,19837.6915,10805.590,9032.1009,19042.1474,10372.25769,8669.889713},
				{null,1995,20813.45171,11337.0871,9476.3645,19326.39177,10527.0856,8799.306174},
				{null,null,0,0,0,0,0,0},
				{null,null,0,0,0,0,0,0},
				{null,"Tabla!RA!","Rama","91120","Desde",1993,null,null},
				{null,"basura",0,0,0,0,0,0},
				{null,null,0,0,0,0,0,0},
				{null,"Años","VBP a precios corrientes","VA a precios corrientes","Consumo Intermedio a precios corrientes","VBP a precios de 1993","VA  a precios de 1993","Consumo Intermedio a precios de 1993"},
				{null,1993,1800,1000,800,1800,1000,800},
				{null,1994,2800,2000,800,3800,2000,1800},
				{null,1995,3800,3000,800,5800,3000,2800},
				{null,1996,4800,4000,800,7800,4000,3800}
			};
			CrearEjemplo(datos,Archivo.CarpetaActual()+@"\temp_borrar\ProdAlim.xls","ProdAlim");
		}
		[Test]
		public void Estamos(){
			cierre.ProcesarLista();
			HojaExcel hResultados=cierre.libro.Hoja(CierrePBG.NombreHojaResultados);
			Assert.AreEqual("Año",hResultados.ValorCelda("A1"));
			Assert.AreEqual(1993,hResultados.ValorCelda("A2"));
			Assert.AreEqual("01110",hResultados.ValorCelda("E2"));
			HojaExcel hResumen=cierre.libro.Hoja(CierrePBG.NombreHojaResumen);
			Assert.AreEqual("Agricultura.xls",hResumen.ValorCelda("B2"));
		}
	}
}
