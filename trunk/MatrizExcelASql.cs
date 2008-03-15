/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 15/03/2008
 * Time: 01:51 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
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
		public MatrizExcelASql(ReceptorSql receptor){
			this.Receptor=receptor;
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
					InsertadorSql insert=new InsertadorSql(Receptor);
					for(int i=0;i<encabezadosFilas.Length;i++){
						insert[camposFilas[i]]=encabezadosFilas[i].TextoCelda(fila,1);
					}
					for(int i=0;i<encabezadosColumnas.Length;i++){
						insert[camposColumnas[i]]=encabezadosColumnas[i].TextoCelda(1,columna);
					}
					insert[campoValor]=matriz.TextoCelda(fila,columna);
					insert.InsertarSiHayCampos();
				}
			}
		}
	}
	[TestFixture]
	public class ProbarMatrizExcelASql{
		string nombreArchivoXLS=Archivo.CarpetaActual()+"\\borrar_prueba_matriz.xls";
		string nombreArchivoMDB=Archivo.CarpetaActual()+"\\temp_Access_Borrar_Matriz.mdb";
		string[,] matriz={
			{"Indice","","año","2001","2002","2002"},
			{"continente","pais","ciudad/trimestremes","4","1","2"},
			{"America","Argentina","Buenos Aires","100","200","210"},
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
			libro.Close();
		}
		[Test]
		public void crearReceptor(){
			Archivo.Borrar(nombreArchivoMDB);
			BaseDatos.CrearMDB(nombreArchivoMDB);
			OleDbConnection con=BaseDatos.abrirMDB(nombreArchivoMDB);
			string sentencia=@"
				CREATE TABLE Receptor(
				   continente varchar(250),
				   pais varchar(250),
				   ciudad varchar(250),
				   [año] varchar(250),
				   trimestre varchar(250),
				   indice varchar(250)
				   )";
			OleDbCommand com=new OleDbCommand(sentencia,con);
			com.ExecuteNonQuery();
			con.Close();
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
					libro.Rango("B3","B4"),
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
			string[,] dumpObtenido=receptor.Dump();
			string[,] dumpEsperado=
				{
					{"America","Argentina","Buenos Aires","2001","4","100"},
					{"America","Argentina","Buenos Aires","2002","1","200"},
					{"America","Argentina","Buenos Aires","2002","2","210"},
					{"America","Uruguay","Montevideo","2001","4","100"},
					{"America","Uruguay","Montevideo","2002","1","120"},
					{"America","Uruguay","Montevideo","2002","2","140"}
				};
			Assert.AreEqual(dumpEsperado,dumpObtenido);
			libro.Close();
		}
	}
	public class ParametrosMatrizExcelASql:Parametros,IParametorsReceptorSql{
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
}
