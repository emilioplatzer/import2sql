/*
 * Creado por SharpDevelop.
 * Usuario: asaccal
 * Fecha: 19/03/2008
 * Hora: 14:02
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;

namespace TodoASql
{
	/// <summary>
	/// Description of UnProcesamiento.
	/// </summary>
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
}
