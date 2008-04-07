/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 15/03/2008
 * Time: 01:02 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;

namespace TodoASql
{
	public interface IParametorsReceptorSql{
		string TablaReceptora{ get; }
		string BaseReceptora{ get; }
	}
	public class ReceptorSql
	{
		internal BaseDatos db;
		IDataReader SelectAbierto;
		internal string NombreTabla;
		public ReceptorSql(BaseDatos db,string nombreTablaReceptora){
			this.db=db;
			this.NombreTabla=nombreTablaReceptora;
			SelectAbierto=db.ExecuteReader("SELECT * FROM "+db.StuffTabla(NombreTabla));
		}
		public ReceptorSql(IParametorsReceptorSql parametros){
			this.NombreTabla=parametros.TablaReceptora;
			AbrirBaseMDB(parametros.BaseReceptora);
		}
		void AbrirBaseMDB(string nombreMDB){
			db=BdAccess.Abrir(nombreMDB);
			SelectAbierto=db.ExecuteReader("SELECT * FROM "+db.StuffTabla(NombreTabla));
		}
		public int FieldCount{
			get{
				return SelectAbierto.FieldCount;
			}
		}
		public string GetName(int i){
			return SelectAbierto.GetName(i);
		}
		public void Close(){
			SelectAbierto.Close();
			db.Close();
		}
		public string[,] DumpString(){
			int registros=(int) db.ExecuteScalar("SELECT count(*) FROM ["+NombreTabla+"]");
			IDataReader sel=db.ExecuteReader("SELECT * FROM ["+NombreTabla+"]");
			int campos=sel.FieldCount;
			string[,] matriz=new string[registros,campos];
			for(int i=0; i<registros; i++){
				sel.Read();
				for(int j=0; j<campos; j++){
					matriz[i,j]=sel.GetString(j);
				}
			}
			return matriz;
		}
		public object[,] DumpObject(){
			int registros=(int) db.ExecuteScalar("SELECT count(*) FROM ["+NombreTabla+"]");
			IDataReader sel=db.ExecuteReader("SELECT * FROM ["+NombreTabla+"]");
			int campos=sel.FieldCount;
			object[,] matriz=new object[registros,campos];
			for(int i=0; i<registros; i++){
				sel.Read();
				for(int j=0; j<campos; j++){
					matriz[i,j]=sel.GetValue(j);
				}
			}
			return matriz;
		}
	}
	public class InsertadorSql:IDisposable
	{
		BaseDatos db;
		string NombreTabla;
		StringBuilder campos=new StringBuilder();
		StringBuilder valores=new StringBuilder();
		Separador coma=new Separador(",");
		public string Sentencia;
		public string GuardarErroresEn;
		UnSoloUso controlar=new UnSoloUso();
		public InsertadorSql(ReceptorSql receptor)
			:this(receptor.db,receptor.NombreTabla)
		{
		}
		public InsertadorSql(BaseDatos db,string NombreTabla){
			this.db=db;
			this.NombreTabla=NombreTabla;
		}
		public object this[string campo]{
			set{
				campos.Append(coma+db.StuffCampo(campo));
				valores.Append(coma.mismo()+db.StuffValor(value));
			}
		}
		bool InsertarSiHayCampos(){
			controlar.Uso();
			if(campos.Length>0){
				Sentencia="INSERT INTO "+db.StuffTabla(NombreTabla)
					+@" ("+campos.ToString()+") VALUES ("+valores.ToString()+")";
				db.ExecuteNonQuery(Sentencia);
				return true;
			}else{
				return false;
			}
		}
		public bool HayCampos{
			get{ return campos.Length>0; }
		}
		public void Dispose(){
			if(GuardarErroresEn!=null){
				try{
					InsertarSiHayCampos();
				}catch(OleDbException ex){
					Archivo.Agregar(GuardarErroresEn,Sentencia+";\n");
					System.Console.WriteLine("No pudo importar "+Sentencia);
				}
			}else{
				InsertarSiHayCampos();
			}
		}
	}
}
