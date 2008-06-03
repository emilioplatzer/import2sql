/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 27/04/2008
 * Time: 05:25 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Data;
using NUnit.Framework;

using Comunes;
using BasesDatos;
using DelOffice;
using ModeladorSql;

namespace DelOffice
{
	#if SinOffice
	#else
	public class RecolectorExcel{
		public RecolectorExcel(){
		}
		public void AgregarCamposSiFaltan(BaseDatos db,string NombreTablaReceptora,string[] NombresCampo){
			IDataReader r=db.ExecuteReader("SELECT * FROM "+db.StuffTabla(NombreTablaReceptora));
			r.Read();
			Lista<string> CamposAAgregar=new Lista<string>();
			foreach(string NombreCampo in NombresCampo){
				if(NombreCampo!=null && NombreCampo!=""){
					try{
						string valor=r[simplificar(NombreCampo)].ToString();
						#pragma warning disable 168
					}catch(System.IndexOutOfRangeException ex){
						#pragma warning restore 168
						System.Console.WriteLine("Campo nuevo: {0}",NombreCampo);
						CamposAAgregar.Add(NombreCampo);
					}
				}
			}
			r.Close();
			foreach(string NCampo in CamposAAgregar){
				db.ExecuteNonQuery(
					"ALTER TABLE "+db.StuffTabla(NombreTablaReceptora)+" ADD COLUMN "+db.StuffCampo(simplificar(NCampo))+" VARCHAR(250);"
				);
			}
		}
		public void LevantarLaLista(BaseDatos db,string NombreTablaReceptora,HojaExcel hoja,int columnaId){
			int fila=2;
			int blancos=0;
			while(blancos<10){
				if(hoja.ValorCelda(fila,columnaId)==null){
					blancos++;
				}else{
					using(InsertadorSql ins=new InsertadorSql(db,NombreTablaReceptora)){
						for(int columna=1; columna<=26; columna++){
							string titulo=simplificar(hoja.TextoCelda(1,columna));
							if(titulo!=null && titulo!=""){
								string valor=hoja.TextoCelda(fila,columna);
								if(valor.Length>=250){
									valor=valor.Substring(0,249);
								}
								ins[titulo]=valor;
							}
						}
					}
				}
				fila++;
			}
		}
		public string simplificar(string nombreCampo){
			return nombreCampo.Replace(".","_").Replace("[","(").Replace("]",")");
		}
		public void Procesar(BaseDatos db,string NombreTablaReceptora,string CarpetaExcel,string NombreCampoIdentificador){
			try{
				db.ExecuteNonQuery(
					"CREATE TABLE "+db.StuffTabla(NombreTablaReceptora)+" ("+
					db.StuffCampo(NombreCampoIdentificador)+" varchar(250));"
				);
				db.ExecuteNonQuery(
					"INSERT INTO "+db.StuffTabla(NombreTablaReceptora)+" ("+
					db.StuffCampo(NombreCampoIdentificador)+") VALUES ('0');"
				);
				System.Console.WriteLine("La base fue creada");
				#pragma warning disable 168
			}catch(System.Data.OleDb.OleDbException ex){
				#pragma warning restore 168
				System.Console.WriteLine("La base ya existe");
			}
			Carpeta dir=new Carpeta(CarpetaExcel);
			dir.ProcesarArchivos("*.xls","procesados","salteados",
			    delegate(string nombreArchivo){
			        LibroExcel libro=LibroExcel.Abrir(nombreArchivo);
			        bool tengoDatos=false;
			        for(int i=1; i<=libro.CantidadHojas; i++){
			        	HojaExcel hoja=libro.Hoja(i);
			        	RangoExcel encontreId=
			        		hoja.Fila(1).BuscarPorFilas(NombreCampoIdentificador);
			        	if(encontreId.EsValido){
			        		System.Console.WriteLine("hoja {0}: col {1}",i,encontreId.NumeroColumna);	
			        		AgregarCamposSiFaltan(db,NombreTablaReceptora,hoja.Rango("A1:Z1").TextoRango1D());
			        		LevantarLaLista(db,NombreTablaReceptora,hoja,encontreId.NumeroColumna);
			        		tengoDatos=true;
			        	}else{
			        		System.Console.WriteLine("hoja {0} sin datos",i);
			        	}
			        }
			        libro.CerrarNoHayCambios();
			        return tengoDatos;
			    }
			);
		}
		public static void Ejemplo(){
			System.Console.WriteLine("Procesando");
			RecolectorExcel r=new RecolectorExcel();
			string carpeta=Archivo.CarpetaActual();
			BdAccess db=BdAccess.Abrir(carpeta+@"\TotalCitas.mdb");
			r.Procesar(db,"citas",carpeta+@"\Excel","CUCID");
			System.Console.WriteLine("Listo!");
		}
	}
	[TestFixture]
	public class prRecolectarExcel{
		string nombreBase=Archivo.CarpetaActual()+@"\BaseRecolectora2.mdb";
		public BaseDatos CrearBaseBlanco(){
			Archivo.Borrar(nombreBase);
			BdAccess.Crear(nombreBase);
			BdAccess db=BdAccess.Abrir(nombreBase);
			return db;
		}
		[Test]
		public void UnExcel(){
			string carpetaExcel=Archivo.CarpetaActual()+@"\temp_borrar\";
			string nombreExcel=carpetaExcel+"pruebaExcelRecolector1.xls";
			Archivo.Borrar(nombreExcel.Replace(@"temp_borrar\",@"temp_borrar\salteados\"));
			Archivo.Borrar(nombreExcel.Replace(@"temp_borrar\",@"temp_borrar\procesados\"));
			object[,] datosHoja1={
				{"Nombre","Apellido","CUCID","FechaVisita","Cantidad"},
				{"pepe","Perez","123456","12/04/2008","1"},
				{"jose","Fernandez",123900,new DateTime(2008,1,12),1},
				{null,null,null,null,null},
				{null,null,null,null,null},
				{null,null,null,null,null},
				{null,null,null,null,null},
				{null,null,null,null,null},
				{"pepe","pepez",111111,"12/02/2008",3}
			};
			object[,] datosHoja2={
				{"Nombre y Apellido","cucid","FechaVisita","Cantidad"},
				{"Maria Marquez",222222,null,null},
				{"Coco Bacile",null,new DateTime(2008,1,12),1},
				{"Marta Martuza",333333,"12/02/2008",3}
			};
			object[,] datosHoja3={
				{"Nombre y Apellido","PEPEID","FechaVisita","Cantidad"},
				{"Maria Marquez",222222,null,null},
				{"Coco Bacile",null,new DateTime(2008,1,12),1},
				{"Marta Martuza",333333,"12/02/2008",3}
			};
			LibroExcel libro=LibroExcel.Nuevo();
			HojaExcel hoja1=libro.Hoja(1);
			hoja1.Rellenar(datosHoja1);
			HojaExcel hoja2=libro.Hoja(2);
			hoja2.Rellenar(datosHoja2);
			HojaExcel hoja3=libro.Hoja(3);
			hoja3.Rellenar(datosHoja3);
			libro.GuardarYCerrar(nombreExcel);
			RecolectorExcel rec=new RecolectorExcel();
			BaseDatos db=CrearBaseBlanco();
			rec.Procesar(db,"receptora",carpetaExcel,"CUCID");
			IDataReader r=db.ExecuteReader("SELECT * FROM receptora ORDER BY CUCID");
			Assert.AreEqual(6,r.FieldCount);
			Assert.AreEqual("CUCID",r.GetName(0));
			r.Read();
			Assert.AreEqual("0",r["CUCID"].ToString());
			r.Read();
			Assert.AreEqual("111111",r["CUCID"].ToString());
			Assert.AreEqual("pepe",r["Nombre"]);
			r.Read();
			Assert.AreEqual("123456",r["CUCID"]);
		}
	}
	#endif
	/*
		1) Recorrer todos los excel de una carpeta. OK
		2) Recorrer cada hoja del excel. 1hs
		3) Buscar en la fila 1 si existe la palabra CUCID. 
		4) Que avise si no encontró CUCID en ninguna hoja. 10'
		5) Verificar que existan todas las columnas de la fila 1 como columnas de la tabla receptora. 30'
		5.1) Agregar la columna que falta (char 250) 10'
		6) Grabar en la base. OK
		7) Si CUCID está en blanco el renglon no se graba. 5'
		8) Después de 10 CUCID en blanco terminar esa hoja 5'
	 */
}
