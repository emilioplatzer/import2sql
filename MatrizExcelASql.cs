/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 15/03/2008
 * Time: 01:51 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
#if SinOffice
#else
using System.Data.OleDb;
using NUnit.Framework;

namespace TodoASql
{
	/// <summary>
	/// Description of MatrizExcelASql.
	/// </summary>
	public class MatrizExcelASql
	{
		ReceptorSql Receptor;
		public string[] CamposFijos;
		public object[] ValoresFijos;
		public MatrizExcelASql(ReceptorSql receptor){
			this.Receptor=receptor;
		}
		InsertadorSql NuevoInsertador(){
			InsertadorSql insert=new InsertadorSql(Receptor);
			if(CamposFijos!=null){
				Assert.AreEqual(CamposFijos.Length,ValoresFijos.Length);
				for(int i=0;i<CamposFijos.Length;i++){
					insert[CamposFijos[i]]=ValoresFijos[i];
				}
			}
			return insert;
		}
		public void PasarHoja(RangoExcel matriz,RangoExcel[] encabezadosFilas, RangoExcel[] encabezadosColumnas, string campoValor, 
		                      string[] camposFilas, string[] camposColumnas)
		{
			int maxFila=matriz.CantidadFilas;
			int maxColumna=matriz.CantidadColumnas;
			Assert.AreEqual(encabezadosFilas.Length,camposFilas.Length);
			Assert.AreEqual(encabezadosColumnas.Length,camposColumnas.Length);
			for(int fila=1;fila<=maxFila;fila++){
				for(int columna=1;columna<=maxColumna;columna++){
					object valor=matriz.ValorCelda(fila,columna);
					if(valor!=null){
						InsertadorSql insert=NuevoInsertador();
						for(int i=0;i<encabezadosFilas.Length;i++){
							insert[camposFilas[i]]=encabezadosFilas[i].ValorCelda(fila,1);
						}
						for(int i=0;i<encabezadosColumnas.Length;i++){
							insert[camposColumnas[i]]=encabezadosColumnas[i].ValorCelda(1,columna);
						}
						insert[campoValor]=valor;
						insert.InsertarSiHayCampos();
					}
				}
			}
		}
		public void PasarHoja(RangoExcel matriz,RangoExcel encabezadosFilas, RangoExcel encabezadosColumnas, string campoValor, 
		                      string[] camposFilas, string[] camposColumnas)
		{
			RangoExcel[] ArrayEncabezadosFilas=new RangoExcel[encabezadosFilas.CantidadColumnas];
			for(int i=0;i<encabezadosFilas.CantidadColumnas;i++){
				ArrayEncabezadosFilas[i]=encabezadosFilas.Columna(i+1);
			}
			RangoExcel[] ArrayEncabezadosColumnas=new RangoExcel[encabezadosColumnas.CantidadFilas];
			for(int i=0;i<encabezadosColumnas.CantidadFilas;i++){
				ArrayEncabezadosColumnas[i]=encabezadosColumnas.Fila(i+1);
			}
			PasarHoja(matriz,ArrayEncabezadosFilas,ArrayEncabezadosColumnas,campoValor,camposFilas,camposColumnas);
		}
		public void PasarHoja(ParametrosMatrizExcelASql parametros){
			if(parametros.TitulosDeFila.NombreArchivo==null){
				parametros.TitulosDeFila.NombreArchivo=parametros.Matriz.NombreArchivo;
			}
			if(parametros.TitulosDeColumna.NombreArchivo==null){
				parametros.TitulosDeColumna.NombreArchivo=parametros.Matriz.NombreArchivo;
			}
			ColeccionExcel libros=new ColeccionExcel();
			libros.Abrir(parametros.Matriz.NombreArchivo);
			libros.Abrir(parametros.TitulosDeFila.NombreArchivo);
			libros.Abrir(parametros.TitulosDeColumna.NombreArchivo);
			PasarHoja(
				libros[parametros.Matriz.NombreArchivo].Rango(parametros.Matriz.Rango),
				libros[parametros.TitulosDeFila.NombreArchivo].Rango(parametros.TitulosDeFila.Rango),
				libros[parametros.TitulosDeColumna.NombreArchivo].Rango(parametros.TitulosDeColumna.Rango),
				parametros.Matriz.Titulos[0],
				parametros.TitulosDeFila.Titulos,
				parametros.TitulosDeColumna.Titulos
			);
		}
	}
	[TestFixture]
	public class ProbarMatrizExcelASql{
		string nombreArchivoXLS=Archivo.CarpetaActual()+"\\borrar_prueba_matriz.xls";
		string nombreArchivoMDB=Archivo.CarpetaActual()+"\\temp_Access_Borrar_Matriz.mdb";
		string[,] matriz={
			{"Indice","","año","2001","2002","2002"},
			{"continente","pais","ciudad/trimestremes","4","1","2"},
			{"America","Argentina","Buenos Aires","100",null,"210"},
			{"America","Uruguay","Montevideo","100","120","140"}
		};
		[Test]
		public void crear(){
			Archivo.Borrar(nombreArchivoXLS);
			LibroExcel libro=LibroExcel.Nuevo();
			libro.Rellenar(matriz);
			libro.GuardarYCerrar(nombreArchivoXLS);
		}
		[Test]
		public void revisar(){
			LibroExcel libro=LibroExcel.Abrir(nombreArchivoXLS);
			Assert.AreEqual("120",libro.TextoCelda("E4"));
			libro.CerrarNoHayCambios();
		}
		[Test]
		public void crearReceptor(){
			Archivo.Borrar(nombreArchivoMDB);
			BdAccess.Crear(nombreArchivoMDB);
			BdAccess db=BdAccess.Abrir(nombreArchivoMDB);
			db.ExecuteNonQuery(@"
				CREATE TABLE Receptor(
				   continente varchar(250),
				   pais varchar(250),
				   ciudad varchar(250),
				   [año] varchar(250),
				   trimestre varchar(250),
				   indice varchar(250)
				   )");
			db.Close();
		}
		[Test]
		public void trasvasar(){
			ParametrosMatrizExcelASql parametros=new ParametrosMatrizExcelASql(nombreArchivoMDB,"Receptor");
			ReceptorSql receptor=new ReceptorSql(parametros);
			MatrizExcelASql matriz=new MatrizExcelASql(receptor);
			LibroExcel libro=LibroExcel.Abrir(nombreArchivoXLS);
			matriz.PasarHoja(
				libro.Rango("D3","F4"),
				new RangoExcel[]{
					libro.Rango("A3","A4"),
					libro.Rango("B3:B4"),
					libro.Rango("C3","C4")
				},new RangoExcel[]{
					libro.Rango("D1","F1"),
					libro.Rango("D2","F2")
				},
				"indice",
				new string[]{
					"continente","pais","ciudad"
				},
				new string[]{
					"año","trimestre"
				}
			);
			string[,] dumpObtenido=receptor.DumpString();
			string[,] dumpEsperado=
				{
					{"America","Argentina","Buenos Aires","2001","4","100"},
					{"America","Argentina","Buenos Aires","2002","2","210"},
					{"America","Uruguay","Montevideo","2001","4","100"},
					{"America","Uruguay","Montevideo","2002","1","120"},
					{"America","Uruguay","Montevideo","2002","2","140"}
				};
			Assert.AreEqual(dumpEsperado,dumpObtenido);
			libro.CerrarNoHayCambios();
		}
		[Test]
		public void trasvasarConParametros(){
			using(BdAccess db=BdAccess.Abrir(nombreArchivoMDB)){
				db.ExecuteNonQuery("delete from Receptor");
			}
			ParametrosMatrizExcelASql parametros=new ParametrosMatrizExcelASql(nombreArchivoMDB,"Receptor");
			parametros.Matriz.NombreArchivo=nombreArchivoXLS;
			parametros.Matriz.Rango="D3:F4";
			parametros.Matriz.Titulos=new string[]{"indice"};
			parametros.TitulosDeFila.NombreArchivo=null;
			parametros.TitulosDeFila.Rango="A3:C4";
			parametros.TitulosDeFila.Titulos=new string[]{
				"continente","pais","ciudad"
			};
			parametros.TitulosDeColumna.NombreArchivo=null;
			parametros.TitulosDeColumna.Rango="D1:F2";
			parametros.TitulosDeColumna.Titulos=new string[]{
				"año","trimestre"
			};
			ReceptorSql receptor=new ReceptorSql(parametros);
			MatrizExcelASql matriz=new MatrizExcelASql(receptor);
			// LibroExcel libro=LibroExcel.Abrir(nombreArchivoXLS);
			matriz.PasarHoja(parametros);
			string[,] dumpObtenido=receptor.DumpString();
			string[,] dumpEsperado=
				{
					{"America","Argentina","Buenos Aires","2001","4","100"},
					{"America","Argentina","Buenos Aires","2002","2","210"},
					{"America","Uruguay","Montevideo","2001","4","100"},
					{"America","Uruguay","Montevideo","2002","1","120"},
					{"America","Uruguay","Montevideo","2002","2","140"}
				};
			Assert.AreEqual(dumpEsperado,dumpObtenido);
			//libro.Close();
		}
	}
	public struct ExcelDefinicionRango{
		public string NombreArchivo;
		public string Rango;
		public string[] Titulos;
	}
	public class ParametrosMatrizExcelASql:Parametros,IParametorsReceptorSql{
		public ExcelDefinicionRango Matriz;
		public ExcelDefinicionRango TitulosDeFila;
		public ExcelDefinicionRango TitulosDeColumna;
		string tablaReceptora; public string TablaReceptora{ get{ return tablaReceptora; }}
		string baseReceptora; public string BaseReceptora{ get{ return baseReceptora; }}
		public ParametrosMatrizExcelASql(LeerPorDefecto queHacer):base(queHacer){
		}
		public ParametrosMatrizExcelASql(string nombreMDB,string nombreTabla)
			:base(LeerPorDefecto.NO)
		{
			this.baseReceptora=nombreMDB;
			this.tablaReceptora=nombreTabla;
		}
	}
	[TestFixture]
	public class ProbarMatrizExcelASqlGenerica{
		string nombreArchivoXLS=Archivo.CarpetaActual()+"\\borrar_prueba_matrizG.xls";
		string nombreArchivoMDB=Archivo.CarpetaActual()+"\\temp_Access_Borrar_MatrizG.mdb";
		object[,] matriz={
			{"Indice","","año",2001,2002,2002},
			{"codigo","cierre","ciudad/trimestremes",4,1,2},
			{"11101",new DateTime(2001,12,20),"Buenos Aires",100.1,200.2,210.3},
			{"11102",new DateTime(2007,6,5),"Montevideo",100.21,120.22,140.23}
		};
		[Test]
		public void crear(){
			Archivo.Borrar(nombreArchivoXLS);
			LibroExcel libro=LibroExcel.Nuevo();
			libro.Rellenar(matriz);
			libro.GuardarYCerrar(nombreArchivoXLS);
		}
		[Test]
		public void revisar(){
			LibroExcel libro=LibroExcel.Abrir(nombreArchivoXLS);
			Assert.AreEqual(120.22,libro.ValorCelda("E4"));
			libro.CerrarNoHayCambios();
		}
		[Test]
		public void crearReceptor(){
			Archivo.Borrar(nombreArchivoMDB);
			BdAccess.Crear(nombreArchivoMDB);
			BdAccess db=BdAccess.Abrir(nombreArchivoMDB);
			db.ExecuteNonQuery(@"
				CREATE TABLE Receptor(
				   lote varchar(10),
				   version integer,
				   codigo varchar(250),
				   cierre date,
				   ciudad varchar(250),
				   [año] integer,
				   trimestre integer,
				   indice double
				   )");
			db.Close();
		}
		[Test]
		public void trasvasar(){
			ParametrosMatrizExcelASql parametros=new ParametrosMatrizExcelASql(nombreArchivoMDB,"Receptor");
			ReceptorSql receptor=new ReceptorSql(parametros);
			MatrizExcelASql matriz=new MatrizExcelASql(receptor);
			LibroExcel libro=LibroExcel.Abrir(nombreArchivoXLS);
			matriz.CamposFijos=new string[]{"lote","version"};
			matriz.ValoresFijos=new object[]{"único",111};
			matriz.PasarHoja(
				libro.Rango("D3","F4"),
				new RangoExcel[]{
					libro.Rango("A3","A4"),
					libro.Rango("B3","B4"),
					libro.Rango("C3","C4")
				},new RangoExcel[]{
					libro.Rango("D1","F1"),
					libro.Rango("D2","F2")
				},
				"indice",
				new string[]{
					"codigo","cierre","ciudad"
				},
				new string[]{
					"año","trimestre"
				}
			);
			object[,] dumpObtenido=receptor.DumpObject();
			object[,] dumpEsperado=
				{
					{"único",111,"11101",new DateTime(2001,12,20),"Buenos Aires",2001,4,100.1},
					{"único",111,"11101",new DateTime(2001,12,20),"Buenos Aires",2002,1,200.2},
					{"único",111,"11101",new DateTime(2001,12,20),"Buenos Aires",2002,2,210.3},
					{"único",111,"11102",new DateTime(2007,6,5),"Montevideo",2001,4,100.21},
					{"único",111,"11102",new DateTime(2007,6,5),"Montevideo",2002,1,120.22},
					{"único",111,"11102",new DateTime(2007,6,5),"Montevideo",2002,2,140.23}
				};
			Assert.AreEqual(dumpEsperado,dumpObtenido);
			libro.CerrarNoHayCambios();
		}
	}
}
#endif
