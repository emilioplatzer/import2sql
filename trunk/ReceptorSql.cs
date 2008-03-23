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
		internal BaseDatos db;
		IDataReader SelectAbierto;
		internal string NombreTabla;
		/*
		public ReceptorSql():this(new ParametrosMailASql(Parametros.LeerPorDefecto.SI)){
		}
		*/
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
					// string valor=(string) sel.GetValue(j+1);
					// matriz[i,j]=valor;
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
	public class InsertadorSql:IDisposable
	{
		BaseDatos db;
		string NombreTabla;
		StringBuilder campos=new StringBuilder();
		StringBuilder valores=new StringBuilder();
		Separador coma=new Separador(",");
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
		public bool InsertarSiHayCampos(){
			controlar.Uso();
			if(campos.Length>0){
				string sentencia="INSERT INTO "+db.StuffTabla(NombreTabla)
					+@" ("+campos.ToString()+") VALUES ("+valores.ToString()+")";
				db.ExecuteNonQuery(sentencia);
				return true;
			}else{
				return false;
			}
		}
		public void Dispose(){
			
		}
	}
}
