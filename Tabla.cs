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
using PartesSql=System.Collections.Generic.List<Modelador.Sqlizable>;

namespace Modelador
{
	public abstract class Tabla:Sqlizable
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
		public override string ToSql(BaseDatos db)
		{
			return db.StuffTabla(this.NombreTabla);
		}
	}
	public abstract class Campo:Sqlizable
	{
		public string Nombre;
		public string NombreCampo;
		public abstract string TipoCampo{ get; }
		public bool EsPk;
		Tabla TablaContenedora;
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
		/*
		public void Entablar(Tabla tabla){ // marcarlo como perteneciente a la tabla
			TablaContenedora=tabla;
		}
		*/
		public virtual ExpresionSql EsNulo(){
			return new ExpresionSql(this,new LiteralSql(" IS NULL"));
		}
		public override string ToSql(BaseDatos db)
		{
			return db.StuffCampo(this.NombreCampo);
		}
		public ExpresionSql Comparado<T>(string OperadorTextual,T expresion){
			return new ExpresionSql(this,new LiteralSql(OperadorTextual),new ValorSql<T>(expresion));
		}
		public ExpresionSql Igual<T>(T expresion){
			return Comparado<T>("=",expresion);
		}
		public ExpresionSql Distinto<T>(T expresion){
			return Comparado<T>("<>",expresion);
		}
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
			return new SentenciaUpdate.Sets(this,new ExpresionSql(new ValorSql<T>(valor)));
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
	public abstract class Sqlizable{
		public abstract string ToSql(BaseDatos db);
	}
	public class LiteralSql:Sqlizable{
		public string Literal;
		public LiteralSql(string Literal){
			this.Literal=Literal;
		}
		public override string ToSql(BaseDatos db){
			return Literal;
		}
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
	}
	public abstract class Sentencia{
		public abstract PartesSql Partes();
	}
	public class SentenciaUpdate:Sentencia{
		PartesSql ParteSet=new PartesSql();
		public SentenciaUpdate(Tabla tabla,Sets primerSet,params Sets[] sets){
			ParteSet.Add(new LiteralSql("UPDATE "));
			ParteSet.Add(tabla);
			ParteSet.Add(new LiteralSql(" SET "));
			ParteSet.Add(primerSet.CampoAsignado);
			ParteSet.Add(new LiteralSql("="));
			ParteSet.Add(primerSet.ValorAsignar);
			foreach(Sets s in sets){
				ParteSet.Add(new LiteralSql(", "));
				ParteSet.Add(s.CampoAsignado);
				ParteSet.Add(new LiteralSql("="));
				ParteSet.Add(s.ValorAsignar);
			}
		}
		public class Sets{
			public Campo CampoAsignado;
			public ExpresionSql ValorAsignar;
			public Sets(Campo CampoAsignado,ExpresionSql ValorAsignar){
				this.CampoAsignado=CampoAsignado;
				this.ValorAsignar=ValorAsignar;
			}
		}
		public SentenciaUpdate Where(ExpresionSql expresion){
			ParteSet.Add(new LiteralSql(" WHERE "));
			ParteSet.Add(expresion);
			return this;
		}
		public override PartesSql Partes(){
			return ParteSet;
		}
	}
	public class Ejecutador:TodoASql.EjecutadorSql{
		public Ejecutador(BaseDatos db)
			:base(db)
		{
		}
		public void Ejecutar(Sentencia s){
			
		}
		public string Dump(Sentencia s){
			StringBuilder rta=new StringBuilder();
			foreach(Sqlizable p in s.Partes()){
				rta.Append(p.ToSql(db));
			}
			rta.Append(";");
			return rta.ToString();
		}
	}
	public class ExpresionSql:Sqlizable{
		PartesSql Partes=new PartesSql();
		public ExpresionSql(params Sqlizable[] Partes){
			this.Partes.AddRange(Partes);
		}
		ExpresionSql(PartesSql Partes){
			this.Partes=Partes;
		}
		public virtual ExpresionSql And(ExpresionSql otra){
			PartesSql nueva=Partes;
			nueva.Add(new LiteralSql(" AND "));
			nueva.AddRange(otra.Partes);
			return new ExpresionSql(nueva);
		}
		public virtual ExpresionSql Or(ExpresionSql otra){
			PartesSql nueva=new PartesSql();
			nueva.Add(new LiteralSql("("));
			nueva.AddRange(Partes);
			nueva.Add(new LiteralSql(" OR "));
			nueva.AddRange(otra.Partes);
			nueva.Add(new LiteralSql(")"));
			return new ExpresionSql(nueva);
		}
		public override string ToSql(BaseDatos db)
		{
			StringBuilder rta=new StringBuilder();
			foreach(Sqlizable s in Partes){
				rta.Append(s.ToSql(db));
			}
			return rta.ToString();
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
			Assert.AreEqual("UPDATE [productos] SET [producto]='P1', [nombreproducto]='Producto 1';",
			                new Ejecutador(dba)
			                .Dump(new SentenciaUpdate(p,p.cProducto.Set("P1"),p.cNombreProducto.Set("Producto 1"))));
			Assert.AreEqual("UPDATE [productos] SET [producto]='P1', [nombreproducto]='Producto 1' WHERE [producto]='P3' AND ([nombreproducto] IS NULL OR [nombreproducto]<>[producto]);",
			                new Ejecutador(dba)
			                .Dump(new SentenciaUpdate(p,p.cProducto.Set("P1"),p.cNombreProducto.Set("Producto 1"))
			                      .Where(p.cProducto.Igual("P3").And(
			                      	p.cNombreProducto.EsNulo().Or(p.cNombreProducto.Distinto(p.cProducto))))));
		}	
		#endif
	}
}
