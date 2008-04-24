/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 04/04/2008
 * Time: 05:38 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Reflection;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using NUnit.Framework;

using Comunes;
using BasesDatos;
using Modelador;

namespace Modelador
{
	public delegate bool Filtro(Campo c);
	public abstract class Tabla:Campable{
		public string NombreTabla;
		public BaseDatos db;
		public int CantidadCamposPk;
		public string Alias;
		public bool IniciadasFk=false;
		public Tabla TablaRelacionada;
		public Diccionario<Campo,ExpresionSql> CamposRelacionadosFk;
		public Lista<Tabla> TablasFk;
		public System.Collections.Generic.Dictionary<string, Campo> CamposFkAlias=new System.Collections.Generic.Dictionary<string, Campo>();
		public Fk.Tipo TipoFk=Fk.Tipo.Obligatoria;
		public bool LiberadaDelContextoDelEjecutador; // Del contexto del ejecutador
		public bool LevantoCampos=false;
		public Tabla()
		{
			Construir();
			NombreTabla=this.GetType().Name.ToLowerInvariant();
		}
		public Tabla(BaseDatos db,params object[] Claves)
			:this()
		{
			Leer(db,Claves);
		}
		public static string NombreFieldANombreCampo(string nombreField){
			return nombreField.Substring(1);
		}
		protected virtual void ConstruirCampos(){
      		Assembly assem = Assembly.GetExecutingAssembly();
      		System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
      				Campo c=(Campo)assem.CreateInstance(m.FieldType.FullName);
      				c.Nombre=NombreFieldANombreCampo(m.Name);
      				c.NombreCampo=c.Nombre.ToLowerInvariant();
      				c.TablaContenedora=this;
      				m.SetValue(this,c);
      				foreach (System.Attribute attr in m.GetCustomAttributes(true)){
      					if(attr is AplicadorCampo){
      						AplicadorCampo apl=attr as AplicadorCampo;
      						apl.Aplicar(ref c);
      					}
      					if(attr is Pk){
      						CantidadCamposPk++;
      					}
      					if(attr is Fk){
      						Fk fk=attr as Fk;
      						if(fk.Alias!=null){
      							CamposFkAlias[fk.Alias]=c;
      						}
      					}
      				}
				}
			}
		}
		protected void Construir(){
			ConstruirCampos();
		}
		public string SentenciaCreateTable(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			StringBuilder pk=new StringBuilder("\t"+"primary key (");
      		System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			rta.AppendLine("create table "+this.NombreTabla+"(");
      		Separador comapk=new Separador(",");
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					rta.AppendLine("\t"+c.NombreCampo+" "+c.TipoCampo+c.Opcionalidad+",");
					if(c.EsPk){
						pk.Append(comapk+c.NombreCampo);
					}
				}
			}
      		pk.Append(")");
      		rta.Append(pk);
      		UsarFk();
      		if(TablasFk!=null){
	      		foreach(Tabla t in TablasFk){
      				if(t.TipoFk==Fk.Tipo.Obligatoria || t.TipoFk!=Fk.Tipo.Sugerida && db.SoportaFkMixta){
		      			StringBuilder camposFkEsta=new StringBuilder();
		      			StringBuilder camposFkOtra=new StringBuilder();
		      			Separador coma=new Separador(",");
		      			foreach(Campo c in t.CamposPk()){
		      				camposFkEsta.Append(coma+t.CamposRelacionadosFk[c].UnicoCampo().NombreCampo);
		      				camposFkOtra.Append(coma.mismo()+c.NombreCampo);
		      			}
		      			rta.Append(",\n\t"+"foreign key ("+camposFkEsta.ToString()+") references "+t.NombreTabla+" ("+camposFkOtra.ToString()+")");
      				}
	      		}
      		}
			rta.AppendLine("\n);");
			return rta.ToString();
		}
		public void InsertarValores(BaseDatos db,params Campable[] Campos){
			Sentencia s=new SentenciaInsert(this).Valores(Campos);
			Ejecutador ej=new Ejecutador(db);
			ej.Ejecutar(s);
		}
		/*
		public void Insertar(BaseDatos db,params object[] Valores){
			using(InsertadorSql ins=new InsertadorSql(db,this.NombreTabla)){
      			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
      			int i=0;
				foreach(FieldInfo m in ms){
				if(i>=Valores.Length) break;
					if(m.FieldType.IsSubclassOf(typeof(Campo))){
						Campo c=(Campo)m.GetValue(this);
						ins[c.Nombre]=Valores[i];
					}
					i++;
      			}
      			// ins.InsertarSiHayCampos();
			}
		}
		*/
		public Insertador Insertar(BaseDatos db){
			return new Insertador(db,this);
		}
		public virtual void Leer(BaseDatos db,params object[] Codigos){
			BuscarYLeer(db,true,Codigos);
		}
		public virtual bool Buscar(BaseDatos db,params object[] Codigos){
			return BuscarYLeer(db,false,Codigos);
		}
		bool BuscarYLeer(BaseDatos db,bool LanzaExcepcion,params object[] Codigos){
			this.db=db;
			List<object> Valores=new List<object>();
			foreach(object o in Codigos){
				if(o is Campo){
					Campo c=o as Campo;
					if(c.ValorSinTipo!=null){
						Valores.Add(c);
					}else if(c.ExpresionBase!=null && c.ExpresionBase.CandidatoAGroupBy==false){
						Valores.Add(c.ExpresionBase);
					}
				}else if(o is Tabla){
					Tabla t=o as Tabla;
					foreach(Campo c in t.CamposPk()){
						Valores.Add(c.ValorSinTipo);
					}
				}else{
					Valores.Add(o);
				}
			}
			int i=0;
			object[] parametros=new object[CantidadCamposPk*2];
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
			if(i>=Valores.Count) break;
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					if(c.EsPk){
						parametros[i*2]=c.NombreCampo;
						parametros[i*2+1]=Valores[i];
					}
				}
				i++;
  			}
  			return BuscarYLeerNoPk(db,LanzaExcepcion,parametros);
		}
		public virtual void LeerNoPk(BaseDatos db,params object[] parametros){
			BuscarYLeerNoPk(db,true,parametros);
		}
		bool BuscarYLeerNoPk(BaseDatos db,bool LanzaExcepcion,params object[] parametros){
			this.db=db;
			Separador whereAnd=new Separador("\n WHERE ","\n AND ");
			StringBuilder clausulaWhere=new StringBuilder();
			for(int i=0;i<parametros.Length;i+=2){
				object valor=parametros[i+1];
				if(valor is Campo){
					valor=(valor as Campo).ValorSinTipo;
				}
				clausulaWhere.Append(whereAnd+parametros[i]+"="+db.StuffValor(valor));
  			}
			IDataReader SelectAbierto=db.ExecuteReader("SELECT * FROM "+db.StuffTabla(NombreTabla)+clausulaWhere+";");
			LevantoCampos=SelectAbierto.Read();
			if(LevantoCampos){
				LevantarCampos(SelectAbierto);
			}else{
				if(LanzaExcepcion){
					throw new SystemException("No existe el campo descripto por "+Objeto.ExpandirMiembros(parametros));
				}
			}
			return LevantoCampos;
		}
		public void LevantarCampos(IDataReader SelectAbierto){
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					System.Console.WriteLine("ver "+c.NombreCampo);
					System.Console.WriteLine("valor "+SelectAbierto[c.NombreCampo]);
					c.AsignarValor(SelectAbierto[c.NombreCampo]);
					c.ExpresionBase=null;
				}
  			}
		}
		public override Lista<Campo> Campos(){
			return Campos(null);
		}
		public virtual Lista<Campo> Campos(Filtro filtro){
			Lista<Campo> rta=new Lista<Campo>();
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					if(filtro==null || filtro(c)){
						rta.Add(c);
					}
				}
  			}
  			return rta;
		}
		public virtual Lista<Campo> CamposPk(){
			return Campos(delegate(Campo c){ return c.EsPk; });
			/*
			Lista<Campo> rta=new Lista<Campo>();
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					if(c.EsPk){
						rta.Add(c);
					}
				}
  			}
  			return rta;
  			*/
		}
		public virtual bool TieneElCampo(Campo campo){
			Lista<Campo> rta=new Lista<Campo>();
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					if(c.Nombre==campo.Nombre){
						return true;
					}
				}
  			}
  			return false;
		}
		public virtual Campo CampoIndirecto(string campoNombre){
			Lista<Campo> rta=new Lista<Campo>();
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					if(c.Nombre==campoNombre){
						return c;
					}
				}
  			}
  			Assert.Fail("Debi� encontrar el campo");
  			return null;
		}
		public virtual Campo CampoIndirecto(Campo campo){
			return CampoIndirecto(campo.Nombre);
		}
		public void EsFkDe(Tabla maestra,Fk.Tipo TipoFk,params Campo[] CamposReemplazo){
			this.TablaRelacionada=maestra;
			int cantidadCamposFk=CamposPk().Count;
			Lista<Campo> CampoAReemplazar=new Lista<Campo>();
			Lista<ExpresionSql> ExpresionDeReemplazo=new Lista<ExpresionSql>();
			CamposRelacionadosFk=new Diccionario<Campo,ExpresionSql>();
			foreach(Campo CampoReemplazo in CamposReemplazo){
				if(CampoReemplazo!=null){
					if(CampoReemplazo.TablaContenedora==maestra){
						CampoAReemplazar.Add(CamposPk()[cantidadCamposFk-1]);
						ExpresionDeReemplazo.Add(new ExpresionSql(CampoReemplazo));
					}else{
						CampoAReemplazar.Add(CampoReemplazo);
						ExpresionDeReemplazo.Add(CampoReemplazo.ExpresionBase);
					}
				}
			}
			foreach(Campo c in CamposPk()){
				if(CampoAReemplazar!=null && CampoAReemplazar.IndexOf(c)>=0){
					CamposRelacionadosFk[c]=ExpresionDeReemplazo[CampoAReemplazar.IndexOf(c)];
				}else{
					CamposRelacionadosFk[c]=new ExpresionSql(maestra.CampoIndirecto(c));
				}
			}
			this.TipoFk=TipoFk;
		}
		public void EsFkDe(Tabla maestra){
			EsFkDe(maestra,Fk.Tipo.Sugerida,null);
		}
		public void EsFkDe(Tabla maestra,params Campo[] CampoReemplazo){
			EsFkDe(maestra,Fk.Tipo.Sugerida,CampoReemplazo);
		}
		public void UsarFk(){
			if(!IniciadasFk){
				TablasFk=new Lista<Tabla>();
      			Assembly assem = Assembly.GetExecutingAssembly();
	  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
				foreach(FieldInfo m in ms){
					if(m.FieldType.IsSubclassOf(typeof(Tabla))){
	  					foreach (System.Attribute attr in m.GetCustomAttributes(true)){
	  						if(attr is Fk){
	  							Fk fk=attr as Fk;
      							Tabla nueva=(Tabla)assem.CreateInstance(m.FieldType.FullName);
      							nueva.EsFkDe(this,fk.TipoFk,(fk.Alias!=null?this.CamposFkAlias[fk.Alias]:null));
      							m.SetValue(this,nueva);
      							TablasFk.Add(nueva);
	  						}
	  					}
	  				}
	  			}
	  			IniciadasFk=true;
			}
		}
		public override string ToSql(BaseDatos db)
		{
			return db.StuffTabla(this.NombreTabla)+(this.Alias==null?"":" "+this.Alias);
		}
		public override bool CandidatoAGroupBy{ 
			get{
				Assert.Fail("No deber�a preguntar si una tabla tiene variables");
				return false; 
			} 
		} // No tiene en el sentido de que no es una expresi�n
		/*
		public ExpresionSql SelectSuma(Campo CampoSumar,ExpresionSql ExpresionWhere){
			return new ExpresionSql.SelectSuma(this,CampoSumar,ExpresionWhere);
		}
		*/
		public ExpresionSql SelectSuma(Campo CampoSumar){
			return new ExpresionSql.SelectSuma(this,CampoSumar);
		}
		public RegistrosEnumerables Todos(BaseDatos db){
			return new RegistrosEnumerables(this,db);
		}
	}
	public class RegistrosEnumerables{
		BaseDatos db;
		Tabla TablaARecorrer;
		public RegistrosEnumerables(Tabla TablaARecorrer,BaseDatos db){
			this.db=db;
			this.TablaARecorrer=TablaARecorrer;
		}
		public IteradorRegistro GetEnumerator(){
			return new IteradorRegistro(TablaARecorrer,db);
		}
	}
	public class IteradorRegistro{
		Tabla RegistroActual;
		IDataReader SelectAbierto;
		BaseDatos db;
		bool HayActual;
		string Sentencia;
		public IteradorRegistro(Tabla TablaBase,BaseDatos db){
			this.db=db;
			this.RegistroActual=TablaBase;
			Sentencia="SELECT * FROM "+db.StuffTabla(TablaBase.NombreTabla);
			Reset();
		}
		public void Reset(){
			SelectAbierto=db.ExecuteReader(Sentencia);
			HayActual=false;
		}
		public bool MoveNext(){
			HayActual=SelectAbierto.Read();
			if(HayActual){
				RegistroActual.LevantarCampos(SelectAbierto);
			}
			return HayActual;
		}
		public Tabla Current{
			get{
				if(HayActual){
					return RegistroActual; 
				}else{
					throw new InvalidOperationException();
				}
			}
		}
		public void Dispose(){
			SelectAbierto.Close();
		}
	}
	/////////////
	public class Vista:System.Attribute{}
	public class Fk:System.Attribute{		
		public enum Tipo { Obligatoria, Mixta/*puede tener alg�n campo null y otro no*/, Sugerida/*solo para los joins*/ };
		public Tipo TipoFk;
		public string Alias;
		public Fk(){}
		public Fk(string Alias):this(Alias,Tipo.Obligatoria){}
		protected Fk(string Alias,Tipo TipoFk){
			this.Alias=Alias;
			this.TipoFk=TipoFk;
		}
	}
	public class FkMixta:Fk{
		public FkMixta(string Alias)
			:base(Alias,Tipo.Mixta)
		{
		}
	}
	public class Insertador:InsertadorSql{
		public Insertador(BaseDatos db,Tabla tabla)
			:base(db,tabla.NombreTabla)
		{}
	}
	public abstract class Sqlizable{
		public abstract string ToSql(BaseDatos db);
		public abstract bool CandidatoAGroupBy{ get; }
	}
	public abstract class Campable:Sqlizable{
		public abstract Lista<Campo> Campos();
	}
	public class LiteralSql:Sqlizable{
		public string Literal;
		public LiteralSql(string Literal){
			this.Literal=Literal;
		}
		public override string ToSql(BaseDatos db){
			return Literal;
		}
		public override bool CandidatoAGroupBy{ get{return false;} }
	}
	public class OperadorConcatenacionIzquierda:Sqlizable{
		public override string ToSql(BaseDatos db){
			return db.OperadorConcatenacionIzquierda;
		}
		public override bool CandidatoAGroupBy{ get{return false;} }
	}
	public class OperadorConcatenacionDerecha:Sqlizable{
		public override string ToSql(BaseDatos db){
			return db.OperadorConcatenacionDerecha;
		}
		public override bool CandidatoAGroupBy{ get{return false;} }
	}
	public class OperadorConcatenacionMedio:Sqlizable{
		public override string ToSql(BaseDatos db){
			return db.OperadorConcatenacionMedio;
		}
		public override bool CandidatoAGroupBy{ get{return false;} }
	}
	public class ValorSql<T>:Sqlizable{
		public T Valor;
		public ValorSql(T Valor){
			this.Valor=Valor;
		}
		public override string ToSql(BaseDatos db){
			if(Valor is Sqlizable){
				Sqlizable s=Valor as Sqlizable;
				return s.ToSql(db);
			}
			return db.StuffValor(Valor);
		}
		public override bool CandidatoAGroupBy{ 
			get{
				if(Valor is Sqlizable){
					return (Valor as Sqlizable).CandidatoAGroupBy;
				}
				return false;
			}
		}
	}
	public class ValorSqlNulo:Sqlizable{
		public ValorSqlNulo(){
		}
		public override string ToSql(BaseDatos db){
			return "null";
		}
		public override bool CandidatoAGroupBy{ get{return false;} }
	}
}
