/*
 * Created by SharpDevelop.
 * User: Gdibella
 * Date: 17/02/2008
 * Time: 09:51 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Text;
using System.Reflection;
using NUnit.Framework;
using ADOX;

using Comunes;

namespace BasesDatos
{
	public class EjecutadorBaseDatos{
		internal IDbConnection con;
		protected IDbCommand cmd;
		static Bitacora bitacora;
		static Bitacora bitacoraAsertos;
		static EjecutadorBaseDatos(){
			bitacora=new Bitacora("i2s_query.sql","i2s_queries.sql");
			bitacoraAsertos=new Bitacora("i2s_Asert.txt","i2s_Asertos.txt");
		}
		protected EjecutadorBaseDatos(IDbConnection con)
		{
			this.con=con;
			if(con!=null){
				cmd=this.con.CreateCommand();
			}
		}
		protected EjecutadorBaseDatos(){
		}
		public void TratarDe(Accion accion){
			try{
				accion();
			}catch(System.Exception ex){
				bitacora.RegistrarAdicional("/* "+ex.Message+"\n*/\n");
				if(cmd.CommandText==null || !cmd.CommandText.StartsWith("DROP")){
					throw;
				}
			}
		}
		public IDataReader ExecuteReader(SentenciaSql sentencia){
			Falla.SiEsNulo(con);
			cmd.CommandText=null;
			IDbCommand cmd_local=con.CreateCommand();
			cmd_local.CommandText=bitacora.RegistrarSql(AdaptarSentecia(sentencia));
			IDataReader rta=null;
			TratarDe(delegate(){
				rta=cmd_local.ExecuteReader();
			});
			return rta;
		}
		public object ExecuteScalar(SentenciaSql sentencia){
			Falla.SiEsNulo(cmd);
			cmd.CommandText=AdaptarSentecia(sentencia);
			object rta=null;
			TratarDe(delegate(){
				rta=cmd.ExecuteScalar();
			});
			return rta;
		}
		public int ExecuteNonQuery(SentenciaSql sentencia){
			Falla.SiEsNulo(cmd);
			cmd.CommandText=bitacora.RegistrarSql(AdaptarSentecia(sentencia));
			int rta=0;
			TratarDe(delegate(){
         		rta=cmd.ExecuteNonQuery();
			});
			return rta;
		}
		public void EjecutrarSecuencia(SentenciaSql secuencia){
			foreach(string sentencia in secuencia.Separar()){
				if(sentencia.Trim(" \t\r\n;".ToCharArray())!=""){
					ExecuteNonQuery(sentencia);
				}
			}
		}
		public bool SinRegistros(string sentencia){
			IDataReader rdr=ExecuteReader(sentencia);
			bool rta=!rdr.Read();
			rdr.Close();
			return rta;
		}
		public void AssertSinRegistros(string explicacion,string sentencia){
			IDataReader rdr=ExecuteReader(bitacora.RegistrarSql(sentencia));
			bool rta=!rdr.Read();
			if(!rta){
				bitacoraAsertos.Registrar(explicacion+"\n"+Dump(rdr,10));
			}
			rdr.Close();
			Advertir.SiEsFalso(rta,explicacion);
		}
		public static string Dump(IDataReader rdr,int maxRegistros){
			StringBuilder rta=new StringBuilder("");
			if(maxRegistros==0) maxRegistros--;
			object[] valores=new object[rdr.FieldCount];
			while(maxRegistros<0 || maxRegistros>0){
				rdr.GetValues(valores);
				Separador s=new Separador(";");
				foreach(object valor in valores){
					rta.Append(s+valor.ToString());
				}
				rta.AppendLine();
			if(!rdr.Read()) break;
				maxRegistros--;
			}
			return rta.ToString();
		}
		protected virtual string AdaptarSentecia(SentenciaSql sentencia){
			return sentencia.ToString();
		}
		public class SentenciaSql{
			protected string sentencia;
			protected SentenciaSql(string sentencia){
				this.sentencia=sentencia;
			}
			public static implicit operator SentenciaSql(string sentencia){
				return new SentenciaSql(sentencia);
			}
			public override string ToString(){
				return sentencia;
			}
			public string[] Separar(){
				return sentencia.Split(';');
			}
		}
		public void CompliarParaControlar(string sentencia){
			if(cmd!=null){
				cmd.CommandText=sentencia;
				cmd.Prepare();
			}
		}
	}
	public enum OperadorBinario{Mas,Menos,Por,Dividido,Concatenado}
	public enum OperadorBinarioRelacional{Igual,Distinto,Mayor,Menor,MayorOIgual,MenorOIgual,Or,And}
	public enum OperadorSufijoLogico{EsNulo,NoEsNulo}
	public enum OperadorAgrupada{Suma,Maximo,Minimo,Promedio,PromedioGeometrico}
	public enum OperadorFuncion{Exp,LogE,Str,Nvl,PrimeraPalabra,SinPrimeraPalabra,Normalizar}
	public abstract class BaseDatos:EjecutadorBaseDatos,IDisposable{
		public enum TipoStuff { Siempre, Inteligente, Nunca };
		public TipoStuff TipoStuffActual=TipoStuff.Inteligente;
		protected BaseDatos(IDbConnection con)
			:base(con)
		{
		}
		protected BaseDatos()
			:base()
		{
		}
		public bool EliminarTablaSiExiste(string nombreTabla){
			Falla.SiEsNulo(cmd);
			try{
				ExecuteNonQuery(" DROP TABLE "+StuffTabla(nombreTabla)+";");
				return true;
			}catch(DbException ex){
				if(ex.Message.StartsWith(ErrorCode_NoExisteTabla)){
					return false; // ok, no exist�a la tabla por eso salto la excepci�n
				}
				throw;
			}
		}
		public bool EliminarVistaSiExiste(string nombreTabla){
			Falla.SiEsNulo(cmd);
			try{
				ExecuteNonQuery("DROP VIEW "+StuffTabla(nombreTabla));
				return true;
			}catch(DbException ex){
				if(ex.Message.StartsWith(ErrorCode_NoExisteVista)){
					return false; // ok, no exist�a la tabla por eso salto la excepci�n
				}
				System.Console.WriteLine("EliminarVistaSiExiste. Ex:"+ex.Message);
				throw;
			}
		}
		/*
		public EjecutadorSql Ejecutador(EjecutadorSql.Parametros[] p){
			return new EjecutadorSql(this,p);
		}
		public EjecutadorSql Ejecutador(params object[] p){
			return new EjecutadorSql(this,p);
		}
		*/
		public virtual string StuffValor<T>(T valor){
			if(valor==null){
				return "null";
			}
			if(valor is String){
				string s=valor as string;
				return "'"+s.Replace("'","''")+"'";
			}else if(valor is bool){
				bool b=(bool)(object)valor;
				return b?"'S'":"'N'";
			}else if(valor is DateTime){
				DateTime d=((DateTime)(object) valor);
				return StuffFecha(d);
			}else if(valor is double){
				double d=((double)(object) valor);
				return d.ToString(Cadena.FormatoPuntoDecimal);
			}else if(valor is Enum){
				return "'"+valor.ToString()+"'";
			}else{
				return valor.ToString();
			}
		}
		public void Close(){
			con.Close();
			con=null;
			cmd=null;
		}
		public void Dispose(){
			Close();
		}
		public string FaltaOperador(object Operador){
			Falla.Detener("En la base de datos "+MarcaMotor+" falta definir "+Operador.ToString());
			return null;
		}
		public static int FaltaPrecedenciaOperador(object Operador){
			Falla.Detener("En las bases de datos falta definir la precedencia de "+Operador.ToString());
			return 0;
		}
		public virtual string OperadorToSql(OperadorBinario Operador){
			switch(Operador){
				case OperadorBinario.Mas: return "+"; 
				case OperadorBinario.Menos: return "-"; 
				case OperadorBinario.Por: return "*"; 
				case OperadorBinario.Dividido: return "/"; 
				default:return FaltaOperador(Operador);
			}
		}
		public static int Precedencia(OperadorBinario Operador){
			switch(Operador){
				case OperadorBinario.Mas: return 6; 
				case OperadorBinario.Menos: return 6; 
				case OperadorBinario.Por: return 7; 
				case OperadorBinario.Dividido: return 7; 
				case OperadorBinario.Concatenado:return 5;
				default:return FaltaPrecedenciaOperador(Operador);
			}
		}
		public static int Precedencia(OperadorBinarioRelacional Operador){
			switch(Operador){
				case OperadorBinarioRelacional.And:return 3;
				case OperadorBinarioRelacional.Or:return 2;
				default: return 4;
			}
		}
		public string OperadorToSql(OperadorBinarioRelacional Operador){
			switch(Operador){
				case OperadorBinarioRelacional.Igual: return "="; 
				case OperadorBinarioRelacional.Distinto: return "<>"; 
				case OperadorBinarioRelacional.Menor: return "<"; 
				case OperadorBinarioRelacional.Mayor: return ">"; 
				case OperadorBinarioRelacional.MenorOIgual: return "<="; 
				case OperadorBinarioRelacional.MayorOIgual: return ">="; 
				case OperadorBinarioRelacional.And:return "\n AND ";
				case OperadorBinarioRelacional.Or:return " OR ";
				default: return FaltaOperador(Operador);
			}
		}
		public string OperadorToSql(OperadorSufijoLogico Operador){
			switch(Operador){
				case OperadorSufijoLogico.EsNulo: return " IS NULL"; 
				case OperadorSufijoLogico.NoEsNulo: return " IS NOT NULL"; 
				default: return FaltaOperador(Operador);
			}
		}
		public virtual string OperadorToSqlPrefijo(OperadorFuncion Operador){
			switch(Operador){
				case OperadorFuncion.Exp: return "EXP(";
				case OperadorFuncion.LogE: return FaltaOperador(Operador);
				default: return FaltaOperador(Operador);
			}
		}
		public virtual string OperadorToSqlPrefijo(OperadorAgrupada Operador){
			switch(Operador){
				case OperadorAgrupada.Suma: return "SUM(";
				case OperadorAgrupada.Maximo: return "MAX(";
				case OperadorAgrupada.Minimo: return "MIN(";
				case OperadorAgrupada.Promedio: return "AVG(";
				case OperadorAgrupada.PromedioGeometrico: 
					return OperadorToSqlPrefijo(OperadorFuncion.Exp)+"AVG("+OperadorToSqlPrefijo(OperadorFuncion.LogE);
				default: return FaltaOperador(Operador);
			}
		}
		public virtual string OperadorToSqlSufijo(OperadorFuncion Operador){
			switch(Operador){
				default: return ")";
			}
		}
		public virtual string OperadorToSqlSufijo(OperadorAgrupada Operador){
			switch(Operador){
				case OperadorAgrupada.PromedioGeometrico:return ")))";
				default: return ")";
			}
		}
		public bool DebeStuffear(string nombreTabla){
			return TipoStuffActual==TipoStuff.Siempre 
			   || TipoStuffActual==TipoStuff.Inteligente
			   && nombreTabla.IndexOfAny("[],.��������������!@#$%^&*'\" /\\}{()<>:;�".ToCharArray())>=0;
		}
		public object Verdadero{ get {return "S";} }
		public object Falso{ get {return "N";} }
		public abstract string ErrorCode_NoExisteTabla{ get; }
		public abstract string ErrorCode_NoExisteVista{ get; }
		public abstract string StuffTabla(string nombreTabla);
		public abstract string StuffFecha(DateTime fecha);
		public virtual string StuffCampo(string nombreCampo){ return StuffTabla(nombreCampo); }
		public virtual bool SoportaFkMixta{ get{ return true;}} 
		public abstract bool UpdateConJoin{ get; }
		public virtual bool UpdateSelectSumViaDSum{ get{ return false; } }
		public virtual bool UpdateSoloUnaTabla{ get{ return false; } }
		public abstract bool InternosForzarAs{ get; } 
		public abstract string MarcaMotor{ get; }
		public abstract bool SubSelectsDeUpdateViaVista{ get; }
		public virtual string PrefijoViewSubSelect{ get{ return "subselect_"; }}
	}
	public class SentenciaSql{
		StringBuilder sentencia;
		BaseDatos db;
		public SentenciaSql(BaseDatos db){
			this.db=db;	
			this.sentencia=new StringBuilder("");
		}
		public SentenciaSql(BaseDatos db,string sentencia){
			this.db=db;	
			this.sentencia=new StringBuilder(sentencia);
		}
		public SentenciaSql Arg(string parametro,object valor){
			sentencia.Replace("{"+parametro+"}",db.StuffValor(valor));
			return this;
		}
		public static implicit operator BaseDatos.SentenciaSql(SentenciaSql s){
			return s.sentencia.ToString();
		}
		public override string ToString()
		{
			return sentencia.ToString();
		}
	}
	public class EjecutadorSql:EjecutadorBaseDatos,IDisposable{
		protected BaseDatos db;
		Parametros[] param;
		public class Parametros{
			public string Parametro;
			public object Valor;
			public Parametros(string Parametro,object Valor){
				this.Parametro=Parametro;
				this.Valor=Valor;
			}
		}
		public EjecutadorSql(BaseDatos db,Parametros[] param)
			:base(db.con)
		{
			this.db=db;
			this.param=param;
		}		
		public EjecutadorSql(BaseDatos db,params object[] paramPlanos)
			:base(db.con)
		{
			this.db=db;
			Assert.AreEqual(0,paramPlanos.Length % 2);
			int cant=paramPlanos.Length/2;
			this.param=new Parametros[cant];
			for(int i=0;i<cant;i++){
				this.param[i]=new Parametros((string) paramPlanos[i*2], paramPlanos[i*2+1]);
			}
		}		
		protected override string AdaptarSentecia(SentenciaSql sentencia)
		{
			BasesDatos.SentenciaSql s=new BasesDatos.SentenciaSql(db,base.AdaptarSentecia(sentencia));
			foreach(Parametros p in param){
				s.Arg(p.Parametro,p.Valor);
			}
			return s.ToString();
		}
		public void Dispose(){
		}
	}
	public class ProbarBaseDatos{
		public static void ObjEnTodasLasBases(BaseDatos db){
			ObjOperacionesSimples(db);
			ObjUsarReceptor(db);
			ObjUsarEjectuador(db);
			ObjOperacionesEstructurales(db);
		}
		public static void ObjOperacionesSimples(BaseDatos db){
			IDataReader rdr=db.ExecuteReader("SELECT * FROM tablaexistente");
			rdr.Read();
			Assert.AreEqual("uno",rdr["texto"]);
			rdr.Close();
			Assert.AreEqual(0.625,db.ExecuteScalar("SELECT 10.0/16 FROM tablaexistente WHERE numero=1"));
			db.EliminarTablaSiExiste("nueva_tabla_prueba");
			db.ExecuteNonQuery(@"CREATE TABLE nueva_tabla_prueba (
				nombre varchar(100),
				numero integer,
				monto double precision,
				fecha date,
				primary key (nombre));
			");
			db.ExecuteNonQuery("INSERT INTO nueva_tabla_prueba (nombre) VALUES ('solo')");
			rdr=db.ExecuteReader("SELECT * FROM nueva_tabla_prueba");
			rdr.Read();
			Assert.AreEqual("solo",rdr["nombre"]);
			rdr.Close();
		}
		public static void ObjUsarReceptor(BaseDatos db){
			string nombreComplejo="zoilo's (el mejor, \"s�\"\\[no]/de este {a�o})\t�Zeus&Poseid�n?$pesos$ @algo #m�s";
			ReceptorSql receptor=new ReceptorSql(db,"nueva_tabla_prueba");
			using(InsertadorSql insertador=new InsertadorSql(receptor)){
				insertador["nombre"]="toto";
				insertador["numero"]=2;
				insertador["monto"]=3.1416;
				insertador["fecha"]=new DateTime(2001,12,20);
				// insertador.InsertarSiHayCampos();
			}
			using(InsertadorSql insertador=new InsertadorSql(receptor)){
				insertador["nombre"]="tute";
				insertador["numero"]=3;
				insertador["monto"]=Math.Exp(1);
				insertador["fecha"]=new DateTime(1969,5,6);
				// insertador.InsertarSiHayCampos();
			}
			using(InsertadorSql insertador=new InsertadorSql(receptor)){
				insertador["nombre"]=nombreComplejo;
				insertador["numero"]=-3;
				insertador["monto"]=Math.PI;
				insertador["fecha"]=new DateTime(2000,1,1);
				// insertador.InsertarSiHayCampos();
			}
			IDataReader rdr=db.ExecuteReader("SELECT * FROM nueva_tabla_prueba ORDER BY nombre");
			rdr.Read();
			rdr.Read();
			Assert.AreEqual(3.1416,rdr["monto"]);
			Assert.AreEqual(new DateTime(2001,12,20),rdr["fecha"]);
			rdr.Read();
			Assert.AreEqual(Math.Exp(1),(double) rdr["monto"],0.00000000000001);
			Assert.AreEqual(new DateTime(1969,5,6),rdr["fecha"]);
			Assert.AreEqual(3,db.ExecuteScalar("SELECT 3"));
			Assert.AreEqual(3.1416,db.ExecuteScalar("SELECT 3.1416"));
			rdr.Read();
			Assert.AreEqual(nombreComplejo,rdr["nombre"]);
			Assert.AreEqual(-3,rdr["numero"]);
			Assert.IsFalse(db.SinRegistros("SELECT * FROM nueva_tabla_prueba ORDER BY nombre"));
			Assert.IsTrue(db.SinRegistros("SELECT * FROM nueva_tabla_prueba WHERE nombre='nadie'"));
			db.AssertSinRegistros("no debe fallar","SELECT * FROM nueva_tabla_prueba WHERE nombre='nadie'");
			SentenciaSql s=new SentenciaSql(db,@"
				SELECT nombre FROM nueva_tabla_prueba WHERE fecha={fecha}
			").Arg("fecha",new DateTime(1969,5,6));
			Assert.AreEqual("tute",db.ExecuteScalar(s));
			DateTime fecha=new DateTime(2001,12,20);
			Assert.AreEqual("toto",db.ExecuteScalar(
				new SentenciaSql(db,@"
					SELECT nombre FROM nueva_tabla_prueba WHERE fecha={fecha}
				").Arg("fecha",fecha)
			));
			
		}
		public static void ObjUsarEjectuador(BaseDatos db){
			using(EjecutadorSql ej=new EjecutadorSql(db,
				new EjecutadorSql.Parametros[]{
					new EjecutadorSql.Parametros("nombre","toto"),
					new EjecutadorSql.Parametros("fecha",new DateTime(1969,5,6))
				} ))
			{
				Assert.AreEqual(1,ej.ExecuteScalar("SELECT count(*) FROM nueva_tabla_prueba WHERE fecha={fecha}"));
                Assert.AreEqual(1,ej.ExecuteScalar("SELECT count(*) FROM nueva_tabla_prueba WHERE nombre={nombre}"));
                Assert.AreEqual(0,ej.ExecuteScalar("SELECT count(*) FROM nueva_tabla_prueba WHERE nombre={nombre} AND fecha={fecha}"));
			}
			using(EjecutadorSql ej=new EjecutadorSql(db,"nombre","tute","fecha",new DateTime(2001,12,20))){
				Assert.AreEqual("toto",ej.ExecuteScalar("SELECT nombre FROM nueva_tabla_prueba WHERE fecha={fecha} ORDER BY nombre"));
                Assert.AreEqual("tute",ej.ExecuteScalar("SELECT nombre FROM nueva_tabla_prueba WHERE nombre={nombre} ORDER BY nombre"));
                Assert.AreEqual(0,ej.ExecuteScalar("SELECT count(*) FROM nueva_tabla_prueba WHERE nombre={nombre} AND fecha={fecha}"));
			}
		}
		public static void ObjOperacionesEstructurales(BaseDatos db){
			db.EliminarVistaSiExiste("v_temporaria_a_borrar");
			db.EliminarTablaSiExiste("temporaria_a_borrar");
			db.ExecuteNonQuery("create table temporaria_a_borrar(texto varchar(100));");
			db.ExecuteNonQuery("create view v_temporaria_a_borrar as select * from temporaria_a_borrar;");
			bool NoFalloYDebia=false;
			try{
				db.EliminarTablaSiExiste("temporaria_a_borrar");
				NoFalloYDebia=true;
			}catch(Exception ex){
				System.Console.WriteLine("Correcto. Dio una excepci�n: "+ex.Message);
			}
			if(NoFalloYDebia && db.GetType()!=typeof(SqLite) && db.GetType()!=typeof(BdAccess)){
				Falla.Detener("Debi� dar error porque no la pudo borrar porque estaba relacionada");
			}
			db.EliminarTablaSiExiste("esta_no_existe");
			db.EliminarVistaSiExiste("v_temporaria_a_borrar");
			db.EliminarTablaSiExiste("temporaria_a_borrar");
		}
		public void MostrarMensaje(){
			System.Console.WriteLine("mensaje");
		}
	}
}
