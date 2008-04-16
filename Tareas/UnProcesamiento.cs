/*
 * Creado por SharpDevelop.
 * Usuario: asaccal
 * Fecha: 19/03/2008
 * Hora: 14:02
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;

using Comunes;
using BasesDatos;
using DelOffice;

#if SinOffice
#else
namespace Tareas
{
	public class UnProcesamiento
	{
		static LibroExcel codigos;
		static string carpeta=@"c:\temp\entrada\";
		public UnProcesamiento()
		{
		}
		public static void ProcesarUno(string nombreArchivoXLS, int anno, int mes){
			ParametrosMatrizExcelASql parametros=new ParametrosMatrizExcelASql(carpeta+"PreciosHistoricos.mdb","Precios");
			ReceptorSql receptor=new ReceptorSql(parametros);
			MatrizExcelASql matriz=new MatrizExcelASql(receptor);
			LibroExcel libro=LibroExcel.Abrir(carpeta+nombreArchivoXLS);
			matriz.CamposFijos=new string[]{"ano","mes"};
			matriz.ValoresFijos=new object[]{anno,mes};
			matriz.PasarHoja(
				libro.Rango("K5","BJ315"),
				new RangoExcel[]{
					codigos.Rango("A5","A315"),
					libro.Rango("I5","I315"),
				},new RangoExcel[]{
					libro.Rango("K2","BJ2"),
					libro.Rango("K3","BJ3")
				},
				"precio",
				new string[]{
					"producto","unidad"
				},
				new string[]{
					"informante","complemento_informante"
				}
			);
			libro.CerrarNoHayCambios();
		}
		public static void Ahora(){
			string[] nombreMeses={
				"Abr06.xls",
				"Ago06.xls",
				"Dic06.xls",
				"Ene06.xls",
				"Feb06.xls",
				"Jul06.xls",
				"Jun06.xls",
				"Mar06.xls",
				"May06.xls",
				"Nov06.xls",
				"Oct06.xls",
				"Sep06.xls"
			};
			int [] numeroMeses={4,8,12,1,2,7,6,3,5,11,10,9};
			codigos=LibroExcel.Abrir(carpeta+"Codigos.xls" );
			for(int i=0;i<12;i++){
				ProcesarUno(nombreMeses[i],2006,numeroMeses[i]);
			}
			codigos.CerrarNoHayCambios();
		}
	}
	public class ProcesoLevantarPlanillas{
		BaseDatos db;
		ReceptorSql receptor;
		MatrizExcelASql matriz;
		LibroExcel libro;
		string NombreArchivo;
		string etiquetaFinal="FIN!";
		bool LevantarParametrica(int cantidadVertice, // o sea arriba a la izquierda como campos únicos
		                         int fila, int columna, // fila columna del primer dato 'util
		                         int primerFila, int primerColumna // primera fila y primera columna con datos (matriz completa sin el vertice)
		                        )
		{
			int filaFin=libro.BuscarPorColumnas(etiquetaFinal).NumeroFila-1;
			int columnaFin=libro.BuscarPorFilas(etiquetaFinal).NumeroColumna-1;
			if(filaFin<fila || columnaFin<columna){
				System.Console.Write(" problema con la etiqueta "+etiquetaFinal+
				                    " no deberia: "+filaFin+"<"+fila+" || "+columnaFin+"<"+columna);
				libro.CerrarNoHayCambios();
				return false;
			}
			string[] camposFijos=new string[]{"formato","origen","fecha_importacion"};
			object[] valoresFijos=new object[]{libro.TextoCelda("A1"),NombreArchivo,DateTime.Now};
			string[] camposFijosMasVertice=new string[camposFijos.Length+cantidadVertice];
			object[] valoresFijosMasVertice=new object[camposFijos.Length+cantidadVertice];
			camposFijos.CopyTo(camposFijosMasVertice,0);
			valoresFijos.CopyTo(valoresFijosMasVertice,0);
			if(cantidadVertice>0){
				libro.Rango(3,1,cantidadVertice+3-1,1).TextoRango1D().CopyTo(camposFijosMasVertice,camposFijos.Length);
				libro.Rango(3,2,cantidadVertice+3-1,2).ValorRango1D().CopyTo(valoresFijosMasVertice,camposFijos.Length);
			}
			matriz.CamposFijos=Objeto.Paratodo(camposFijosMasVertice,Cadena.Simplificar);
			matriz.ValoresFijos=valoresFijosMasVertice;
			// matriz.BuscarFaltantes=true;
			matriz.PasarHoja(libro.Rango(fila,columna,filaFin,columnaFin)
			                 ,libro.Rango(fila,primerColumna,filaFin,columna-2)
			                 ,libro.Rango(primerFila,columna,fila-2,columnaFin)
			                 ,"precio"
			                 ,Objeto.Paratodo(libro.Rango(fila-1,primerColumna,fila-1,columna-2).TextoRango1D(),Cadena.Simplificar)
			                 ,Objeto.Paratodo(libro.Rango(primerFila,columna-1,fila-2,columna-1).TextoRango1D(),Cadena.Simplificar));
			libro.CerrarNoHayCambios();
			return true;			
		}
		public bool LevantarPlanilla(string nombreArchivo){
			receptor=new ReceptorSql(db,"PreciosImportados");
			matriz=new MatrizExcelASql(receptor);
			libro=LibroExcel.Abrir(nombreArchivo);
			matriz.GuardarErroresEn=@"c:\temp\indice\Campo\Bases\ErroresDeImportacion.sql";
			NombreArchivo=nombreArchivo;
			if(libro.TextoCelda("A1")=="PER/PROD/INF"){
				return LevantarParametrica(3,8,8,4,1);
			}else if(libro.TextoCelda("A1")=="PROD.INF/PER"){
				return LevantarParametrica(0,8,10,3,1);
			}else if(libro.TextoCelda("A1")=="PROD/PER.INF"){
				return LevantarParametrica(0,10,8,3,1);
			}else{
				libro.CerrarNoHayCambios();
				System.Console.Write(" no es un formato valido reconocido");
				return false;
			}
		}
		public void TraerPlanillasRecepcion(){
			Carpeta dir=new Carpeta(@"c:\temp\indice\Campo\RecepcionPura\");
			db=BdAccess.Abrir(@"c:\temp\indice\Campo\Bases\PreciosRecibidos.mdb");
			// CrearUnaTabla231(db);
			dir.ProcesarArchivos("*.xls","procesado",LevantarPlanilla);
		}
		public static void CrearUnaTabla231(BaseDatos db){
			db.EjecutrarSecuencia(@"
				create table PreciosImportados(
				Ano integer,
				Mes integer,
				Semana integer,
				Cod_Info integer,
				Informante varchar(100),
				Fecha date,
				Cod_Prod varchar(10),
				Nombre varchar(100),
				Especificacion varchar(250),
				Variedad varchar(250),
				Tamano varchar(100),
				Unidad varchar(100),
				Precio varchar(200),
				Formato varchar(100),
				Origen varchar(250),
				Fecha_Importacion date)
			");
		}
	}
}
#endif
