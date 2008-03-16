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
using System.Data.OleDb;

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
		internal OleDbConnection ConexionABase;
		OleDbDataReader SelectAbierto;
		internal string NombreTabla;
		/*
		public ReceptorSql():this(new ParametrosMailASql(Parametros.LeerPorDefecto.SI)){
		}
		*/
		public ReceptorSql(IParametorsReceptorSql parametros){
			this.NombreTabla=parametros.TablaReceptora;
			AbrirBase(parametros.BaseReceptora);
		}
		void AbrirBase(string nombreMDB){
			ConexionABase = BaseDatos.abrirMDB(nombreMDB);
			OleDbCommand cmd = new OleDbCommand("SELECT * FROM ["+NombreTabla+"]",ConexionABase);
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
			OleDbCommand cmd = new OleDbCommand("SELECT count(*) FROM ["+NombreTabla+"]",ConexionABase);
			int registros=(int) cmd.ExecuteScalar();
			cmd = new OleDbCommand("SELECT * FROM ["+NombreTabla+"]",ConexionABase);
			OleDbDataReader sel=cmd.ExecuteReader();
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
			OleDbCommand cmd = new OleDbCommand("SELECT count(*) FROM ["+NombreTabla+"]",ConexionABase);
			int registros=(int) cmd.ExecuteScalar();
			cmd = new OleDbCommand("SELECT * FROM ["+NombreTabla+"]",ConexionABase);
			OleDbDataReader sel=cmd.ExecuteReader();
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
	public class InsertadorSql
	{
		ReceptorSql Receptor;
		StringBuilder campos=new StringBuilder();
		StringBuilder valores=new StringBuilder();
		Separador coma=new Separador(",");
		public InsertadorSql(ReceptorSql receptor){
			this.Receptor=receptor;	
		}
		public string this[string campo]{
			set{
				campos.Append(coma+"["+campo+"]");
				valores.Append(coma.mismo()+'"'+Cadena.SacarComillas(value)+'"');
			}
		}
		public bool InsertarSiHayCampos(){
			if(campos.Length>0){
				string sentencia="INSERT INTO ["+Receptor.NombreTabla+@"] ("+campos.ToString()+") VALUES ("+
						valores.ToString()+")";
				OleDbCommand cmd = new OleDbCommand(sentencia,Receptor.ConexionABase);
				Archivo.Escribir(System.Environment.GetEnvironmentVariable("TEMP")
				                      + @"query.sql"
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
