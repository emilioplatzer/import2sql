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
// using System.Data.OleDb;

namespace TodoASql
{
	public interface IParametorsReceptorSql{
		string TablaReceptora{ get; }
		string BaseReceptora{ get; }
	}
	/// <summary>
	/// Description of ReceptorSql.
	/// </summary>
	public class ReceptorSql
	{
		internal IDbConnection ConexionABase;
		IDataReader SelectAbierto;
		internal string NombreTabla;
		/*
		public ReceptorSql():this(new ParametrosMailASql(Parametros.LeerPorDefecto.SI)){
		}
		*/
		public ReceptorSql(IDbConnection conexion,string nombreTablaReceptora){
			this.ConexionABase=conexion;
			this.NombreTabla=nombreTablaReceptora;
			IDbCommand com=ConexionABase.CreateCommand();
			com.CommandText="SELECT * FROM "+NombreTabla;
			SelectAbierto=com.ExecuteReader();
		}
		public ReceptorSql(IParametorsReceptorSql parametros){
			this.NombreTabla=parametros.TablaReceptora;
			AbrirBase(parametros.BaseReceptora);
		}
		void AbrirBase(string nombreMDB){
			ConexionABase=BaseDatos.abrirMDB(nombreMDB);
			IDbCommand cmd=ConexionABase.CreateCommand();
			cmd.CommandText="SELECT * FROM ["+NombreTabla+"]";
			SelectAbierto=cmd.ExecuteReader();
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
			ConexionABase.Close();
		}
		public string[,] DumpString(){
			IDbCommand cmd=ConexionABase.CreateCommand();
			cmd.CommandText="SELECT count(*) FROM ["+NombreTabla+"]";
			int registros=(int) cmd.ExecuteScalar();
			cmd.CommandText="SELECT * FROM ["+NombreTabla+"]";
			IDataReader sel=cmd.ExecuteReader();
			int campos=sel.FieldCount;
			string[,] matriz=new string[registros,campos];
			for(int i=0; i<registros; i++){
				sel.Read();
				for(int j=0; j<campos; j++){
					// string valor=(string) sel.GetValue(j+1);
					// matriz[i,j]=valor;
					matriz[i,j]=sel.GetString(j);
				}
			}
			return matriz;
		}
		public object[,] DumpObject(){
			IDbCommand cmd=ConexionABase.CreateCommand();
			cmd.CommandText="SELECT count(*) FROM ["+NombreTabla+"]";
			int registros=(int) cmd.ExecuteScalar();
			cmd.CommandText="SELECT * FROM ["+NombreTabla+"]";
			IDataReader sel=cmd.ExecuteReader();
			int campos=sel.FieldCount;
			object[,] matriz=new object[registros,campos];
			for(int i=0; i<registros; i++){
				sel.Read();
				for(int j=0; j<campos; j++){
					// string valor=(string) sel.GetValue(j+1);
					// matriz[i,j]=valor;
					matriz[i,j]=sel.GetValue(j);
				}
			}
			return matriz;
		}
	}
	/*
	public class InsertadorSqlViaDataSet
	{
		ReceptorSql Receptor;
		OleDbDataAdapter Adaptador;
		System.Data.DataTable Tabla;
		DataRow Registro;
		bool HayCamposInsertados;
		public InsertadorSqlViaDataSet(ReceptorSql receptor){
			Tabla=new System.Data.DataTable();
			this.Receptor=receptor;	
			string select="select * from "+receptor.NombreTabla;
			this.Adaptador=new OleDbDataAdapter(select,Receptor.ConexionABase);
			this.Adaptador.Fill(Tabla);
			HayCamposInsertados=false;
			Registro=Tabla.NewRow();
		}
		public object this[string campo]{
			set{
				Registro[campo]=value;
				HayCamposInsertados=true;
			}
		}
		public bool InsertarSiHayCampos(){
			if(HayCamposInsertados){
				HayCamposInsertados=false;
				Tabla.Rows.Add(Registro);
				Adaptador.Update(Tabla);
				return true;
			}else{
				return false;
			}
		}
	}
	*/
	public class InsertadorSql 
	{
		ReceptorSql Receptor;
		StringBuilder campos=new StringBuilder();
		StringBuilder valores=new StringBuilder();
		Separador coma=new Separador(",");
		public InsertadorSql(ReceptorSql receptor){
			this.Receptor=receptor;	
		}
		public object this[string campo]{
			set{
				campos.Append(coma+"["+campo+"]");
				valores.Append(coma.mismo()+Cadena.ParaSql(value));
			}
		}
		public bool InsertarSiHayCampos(){
			if(campos.Length>0){
				string sentencia="INSERT INTO ["+Receptor.NombreTabla+@"] ("+campos.ToString()+") VALUES ("+
						valores.ToString()+")";
				IDbCommand cmd=Receptor.ConexionABase.CreateCommand();
				cmd.CommandText=sentencia;
				Archivo.Escribir(System.Environment.GetEnvironmentVariable("TEMP")
				                      + @"\query.sql"
				                      ,sentencia);
				System.Console.WriteLine(sentencia);
				cmd.ExecuteNonQuery();
				return true;
			}else{
				return false;
			}
		}
	}
}
