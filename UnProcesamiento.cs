/*
 * Creado por SharpDevelop.
 * Usuario: asaccal
 * Fecha: 19/03/2008
 * Hora: 14:02
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;

#if SinOffice
#else
namespace TodoASql
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
		public bool LevantarPlanilla(string nombreArchivo){
			receptor=new ReceptorSql(db,"PreciosImportados");
			MatrizExcelASql matriz=new MatrizExcelASql(receptor);
			LibroExcel libro=LibroExcel.Abrir(nombreArchivo);
			matriz.GuardarErroresEn=@"c:\temp\indice\Campo\Bases\ErroresDeImportacion.sql";
			if(libro.TextoCelda("A1")=="PLAN PREC"){
				string[] camposFijos=new string[]{"formato","origen","fecha_importacion","","",""};
				object[] valoresFijos=new object[]{libro.TextoCelda("A1"),nombreArchivo,DateTime.Now,null,null,null};
				libro.Rango("A3:A5").TextoRango1D().CopyTo(camposFijos,3);
				libro.Rango("B3:B5").ValorRango1D().CopyTo(valoresFijos,3);
				matriz.CamposFijos=Objeto.Paratodo(camposFijos,Cadena.Simplificar);
				matriz.ValoresFijos=valoresFijos;
				matriz.PasarHoja(libro.Rango("H8:Z172")
				                 ,libro.Rango("A8:F172")
				                 ,libro.Rango("H4:Z6")
				                 ,"precio"
				                 ,Objeto.Paratodo(libro.Rango("A7:F7").TextoRango1D(),Cadena.Simplificar)
				                 ,Objeto.Paratodo(libro.Rango("G4:G6").TextoRango1D(),Cadena.Simplificar));
			}else if(libro.TextoCelda("A1")=="PLAN PROD.INF/PER"){
				string[] camposFijos=new string[]{"formato","origen","fecha_importacion"};
				object[] valoresFijos=new object[]{libro.TextoCelda("A1"),nombreArchivo,DateTime.Now};
				matriz.CamposFijos=Objeto.Paratodo(camposFijos,Cadena.Simplificar);
				matriz.ValoresFijos=valoresFijos;
				matriz.PasarHoja(libro.Rango("J8:Q100")
				                 ,libro.Rango("A8:H100")
				                 ,libro.Rango("J3:Q6")
				                 ,"precio"
				                 ,Objeto.Paratodo(libro.Rango("A7:H7").TextoRango1D(),Cadena.Simplificar)
				                 ,Objeto.Paratodo(libro.Rango("I3:I6").TextoRango1D(),Cadena.Simplificar));
			}else if(libro.TextoCelda("A1")=="PLAN PROD/PER.INF"){
				string[] camposFijos=new string[]{"formato","origen","fecha_importacion"};
				object[] valoresFijos=new object[]{libro.TextoCelda("A1"),nombreArchivo,DateTime.Now};
				matriz.CamposFijos=Objeto.Paratodo(camposFijos,Cadena.Simplificar);
				matriz.ValoresFijos=valoresFijos;
				matriz.PasarHoja(libro.Rango("H10:Q100")
				                 ,libro.Rango("A10:F100")
				                 ,libro.Rango("H3:Q8")
				                 ,"precio"
				                 ,Objeto.Paratodo(libro.Rango("A9:F9").TextoRango1D(),Cadena.Simplificar)
				                 ,Objeto.Paratodo(libro.Rango("G3:G8").TextoRango1D(),Cadena.Simplificar));
			}else{
				System.Console.Write(" no es un formato valido reconocido");
				return false;
			}
			libro.CerrarNoHayCambios();
			return true;
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
