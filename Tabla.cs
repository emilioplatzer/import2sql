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
using NUnit.Framework;
using TodoASql;
using Modelador;
using Indices;

namespace Modelador
{
	public abstract class Tabla
	{
		public string NombreTabla;
		public BaseDatos db;
		public int CantidadCamposPk;
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
		protected virtual void ConstruirCampos(){
      		Assembly assem = Assembly.GetExecutingAssembly();
      		System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
      				Campo c=(Campo)assem.CreateInstance(m.FieldType.FullName);
      				c.Nombre=m.Name.Substring(1);
      				c.NombreCampo=c.Nombre.ToLowerInvariant();
      				m.SetValue(this,c);
      				foreach (System.Attribute attr in m.GetCustomAttributes(true)){
      					if(attr is AplicadorCampo){
      						AplicadorCampo apl=attr as AplicadorCampo;
      						apl.Aplicar(ref c);
      					}
      					if(attr is Pk){
      						CantidadCamposPk++;
      					}
      				}
				}
			}
		}
		protected void Construir(){
			ConstruirCampos();
		}
		public string SentenciaCreateTable(){
			StringBuilder rta=new StringBuilder();
			StringBuilder pk=new StringBuilder("primary key (");
      		System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			rta.AppendLine("create table "+this.NombreTabla+"(");
      		Separador comapk=new Separador(",");
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					rta.AppendLine("\t"+c.NombreCampo+" "+c.TipoCampo+",");
					if(c.EsPk){
						pk.Append(comapk+c.NombreCampo);
					}
				}
			}
      		pk.AppendLine(")");
      		rta.Append(pk);
			rta.AppendLine(");");
			return rta.ToString();
		}
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
		public Insertador Insertar(BaseDatos db){
			return new Insertador(db,this);
		}
		public virtual Tabla Leer(BaseDatos db,params object[] Codigos){
			this.db=db;
			int i=0;
			object[] parametros=new object[CantidadCamposPk*2];
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
			if(i>=Codigos.Length) break;
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					if(c.EsPk){
						parametros[i*2]=c.NombreCampo;
						parametros[i*2+1]=Codigos[i];
					}
				}
				i++;
  			}
  			return LeerNoPk(db,parametros);
		}
		public virtual Tabla LeerNoPk(BaseDatos db,params object[] parametros){
			this.db=db;
			Separador whereAnd=new Separador(" WHERE "," AND ");
			StringBuilder clausulaWhere=new StringBuilder();
			for(int i=0;i<parametros.Length;i+=2){
				object valor=parametros[i+1];
				if(valor is Campo){
					valor=(valor as Campo).ValorSinTipo;
				}
				clausulaWhere.Append(whereAnd+parametros[i]+"="+db.StuffValor(valor));
  			}
			IDataReader SelectAbierto=db.ExecuteReader("SELECT * FROM "+db.StuffTabla(NombreTabla)+clausulaWhere);
			SelectAbierto.Read();
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					System.Console.WriteLine("ver "+c.NombreCampo);
					System.Console.WriteLine("valor "+SelectAbierto[c.NombreCampo]);
					c.AsignarValor(SelectAbierto[c.NombreCampo]);
				}
  			}
			return this;
		}
	}
	public abstract class Campo 
	{
		public string Nombre;
		public string NombreCampo;
		public abstract string TipoCampo{ get; }
		public bool EsPk;
		public Campo(){
		}
		public object this[InsertadorSql ins]{
			set{
				if(value is Campo){
					ins[this.NombreCampo]=(value as Campo).ValorSinTipo;
				}else{
					ins[this.NombreCampo]=value;
				}
			}
		}
		public abstract object ValorSinTipo{ get; }
		public abstract void AsignarValor(object valor);
	}
	public class CampoTipo<T>:Campo{
		protected T valor;
		public virtual T Valor{ get{ return valor;} }
		public override object ValorSinTipo{ get{ return valor;} }
		public override string TipoCampo{ 
			get {
				if(valor is int || valor is int?){
					return "integer";
				}else if(valor is string){
					return "varchar";
				}else if(valor is double){
					return "double precision";
				}else{
					return typeof(T).Name; 
				}
			} 
		}
		public CampoTipo()
		{	
		}
		public override void AsignarValor(object valor){
			if(valor is DBNull){
				valor=null;
			}
			this.valor=(T)valor;
		}
		#if SuperSql
		public virtual SentenciaUpdate.Sets Set(T valor){
			return new SentenciaUpdate.Sets(this.NombreCampo,valor);
		}
		#endif                   
	}
	public class CampoPkTipo<T>:CampoTipo<T>{
		public CampoPkTipo()
		{	
			EsPk=true;
		}
	}
	public class CampoEntero:Modelador.CampoTipo<int>{
		public override string TipoCampo{ 
			get { return "integer"; }
		}
	};
	public class CampoEnteroOpcional:Modelador.CampoTipo<int?>{
		public override string TipoCampo{ 
			get { return "integer"; }
		}
	};
	public class CampoChar:Modelador.CampoTipo<string>{
		public int Largo;
		protected CampoChar(int largo){
			this.Largo=largo;	
		}
		public override string TipoCampo{ 
			get { return "varchar("+Largo.ToString()+")"; }
		}
	};
	public class CampoReal:Modelador.CampoTipo<double>{};
	public class CampoLogico:Modelador.CampoChar{
		public CampoLogico():base(1){}
	}
	/////////////
	public class Vista:System.Attribute{}
	public abstract class AplicadorCampo:System.Attribute{
	   	public abstract void Aplicar(ref Campo campo);
	}
	public class Pk:AplicadorCampo{
	   	public override void Aplicar(ref Campo campo){
	   		campo.EsPk=true;
	    }
	}
	public class Insertador:InsertadorSql{
		public Insertador(BaseDatos db,Tabla tabla)
			:base(db,tabla.NombreTabla)
		{}
	}
	#if SuperSql
	public abstract class PartesSentencia{
		public abstract string ToSql(BaseDatos db);
	}
	public class SentenciaUpdate{
		BaseDatos db;
		string parteUpdate;
		public SentenciaUpdate(BaseDatos db,Tabla tabla,Sets primerSet,params Sets[] sets){
			this.db=db;
			parteUpdate="UPDATE "+this.db.StuffTabla(tabla.NombreTabla)+" SET "+primerSet.ToSql(this.db);
			foreach(Sets s in sets){
				parteUpdate+=","+s.ToSql(this.db);
			}
		}
		public class Sets:PartesSentencia{
			string NombreCampo;
			object Valor;
			public Sets(string nombreCampo,object valor){
				this.NombreCampo=nombreCampo;
				this.Valor=valor;
			}
			public override string ToSql(BaseDatos db){
				return db.StuffCampo(NombreCampo)+"="+db.StuffValor(Valor);
			}
		}
		public string ToSql(){
			return parteUpdate+";";
		}
	}
	public class Ejecutador:EjecutadorSql{
		public Ejecutador(BaseDatos db)
			:base(db)
		{
		}
		public SentenciaUpdate Update(Tabla tabla,SentenciaUpdate.Sets primerSet,params SentenciaUpdate.Sets[] sets){
			return new SentenciaUpdate(db,tabla,primerSet,sets);
		}
	}
	#endif
}
namespace PrModelador
{
	public class Periodos:Tabla{
		[Pk] public CampoEntero cAno;
		[Pk] public CampoEntero cMes;
		public CampoEntero cAnoAnt;
		public CampoEntero cMesAnt;
	}
	[TestFixture]
	public class prTabla{
		public prTabla(){
		}
		class Productos:Tabla{
			[Pk] public CampoProducto cProducto;
			public CampoNombre cNombreProducto;
		}
		[Test]
		public void Periodos(){
			Periodos p=new Periodos();
			Assert.AreEqual(0,p.cAno.Valor);
			Assert.AreEqual("Ano",p.cAno.Nombre);
			Assert.AreEqual("create table periodos(ano integer,mes integer,anoant integer,mesant integer,primary key(ano,mes));"
			                ,Cadena.Simplificar(p.SentenciaCreateTable()));
			Productos pr=new Productos();
			Assert.AreEqual("create table productos(producto varchar(4),nombreproducto varchar(250),primary key(producto));"
			                ,Cadena.Simplificar(pr.SentenciaCreateTable()));
		}
		#if SuperSql
		[Test]
		public void SentenciaUpdate(){
			Productos p=new Productos();
			BaseDatos dba=BdAccess.SinAbrir();
			Assert.AreEqual("UPDATE [productos] SET [producto]='P1',[nombreproducto]='Producto 1';",
			                new Ejecutador(dba).Update(p,p.cProducto.Set("P1"),p.cNombreProducto.Set("Producto 1")).ToSql());
			
		}
		#endif
	}
}
