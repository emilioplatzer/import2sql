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
using TablasSql=System.Collections.Generic.List<Modelador.Tabla>;
using CamposSql=System.Collections.Generic.List<Modelador.Campo>;

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
		public virtual CamposSql CamposPk(){
			CamposSql rta=new CamposSql();
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
		}
		public virtual bool TieneElCampo(Campo campo){
			CamposSql rta=new CamposSql();
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
		public virtual Campo CampoIndirecto(Campo campo){
			CamposSql rta=new CamposSql();
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					if(c.Nombre==campo.Nombre){
						return c;
					}
				}
  			}
  			Assert.Fail("Debió encontrar el campo");
  			return null;
		}
		public override string ToSql(BaseDatos db)
		{
			return db.StuffTabla(this.NombreTabla);
		}
		public ExpresionSql SelectSuma(Campo CampoSumar,ExpresionSql ExpresionWhere){
			return new ExpresionSql.SelectSuma(this,CampoSumar,ExpresionWhere);
		}
	}
	public abstract class Campo:Sqlizable{
		public string Nombre;
		public string NombreCampo;
		public abstract string TipoCampo{ get; }
		public bool EsPk;
		public Tabla TablaContenedora;
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
		public ExpresionSql ExpresionBase;
		public Campo Es(ExpresionSql expresion){
			ExpresionBase=expresion;
			return this;
		}
		public Campo Es(Campo campo){
			return Es(new ExpresionSql(campo));
		}
	}
	public class CampoTipo<T>:Campo{
		protected T valor;
		public virtual T Valor{ get{ return valor;} set{ valor=value; } }
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
		public virtual SentenciaUpdate.Sets Set(ExpresionSql expresion){
			return new SentenciaUpdate.Sets(this,expresion);
		}
		public virtual SentenciaUpdate.Sets Set(Campo campo){
			return new SentenciaUpdate.Sets(this,new ExpresionSql(campo));
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
		PartesSql ParteWhere=new PartesSql();
		public abstract PartesSql Partes();
		public abstract TablasSql Tablas();
		public Sentencia Where(ExpresionSql expresion){
			if(ParteWhere.Count>0){
				ParteWhere.Add(new LiteralSql(" AND "));
			}
			ParteWhere.Add(expresion);
			return this;
		}
		public PartesSql PartesWhere(){
			PartesSql rta=new PartesSql();
			if(ParteWhere.Count>0){
				rta.Add(new LiteralSql(" WHERE "));
				rta.AddRange(ParteWhere);
			}
			return rta;
		}
	}
	public class SentenciaUpdate:Sentencia{
		Tabla TablaBase;
		PartesSql ParteSet=new PartesSql();
		public SentenciaUpdate(Tabla tabla,Sets primerSet,params Sets[] sets){
			TablaBase=tabla;
			ParteSet.Add(new LiteralSql("UPDATE "));
			ParteSet.Add(TablaBase);
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
		public override TablasSql Tablas(){
			TablasSql rta=new TablasSql();
			rta.Add(TablaBase);
			return rta;
		}
		public override PartesSql Partes(){
			PartesSql todas=new PartesSql();
			todas.AddRange(ParteSet);
			todas.AddRange(PartesWhere());
			return todas;
		}
	}
	public class ParteSeparadora{
		string Comenzador;
		string Separador;
		bool esPrimero=true;
		public ParteSeparadora(string Comenzador,string Separador){
			this.Comenzador=Comenzador;
			this.Separador=Separador;
		}
		public ParteSeparadora(string Separador){
			this.Separador=Separador;
		}
		public void AgregarEn(PartesSql Partes,params Sqlizable[] Parte){
			if(esPrimero){
				if(Comenzador!=null){
					Partes.Add(new LiteralSql(Comenzador));
				}
				esPrimero=false;
			}else{
				Partes.Add(new LiteralSql(Separador));
			}
			Partes.AddRange(Parte);
		}
	}
	public class SentenciaSelect:Sentencia{
		TablasSql TablasUsadas=new TablasSql();
		protected CamposSql Campos=new CamposSql();
		public SentenciaSelect(params Campo[] Campos){
			ParteSeparadora coma=new ParteSeparadora(", ");
			this.Campos.AddRange(Campos);
		}
		public override TablasSql Tablas(){
			return TablasUsadas;
		}
		public override PartesSql Partes(){
			PartesSql todas=new PartesSql();
			todas.Add(new LiteralSql("SELECT "));
			{
				ParteSeparadora coma=new ParteSeparadora(", ");
				foreach(Campo c in Campos){
					ExpresionSql expresion=c.ExpresionBase;
					if(expresion!=null){
						coma.AgregarEn(todas,expresion,new LiteralSql(" AS "),c);
					}else{
						coma.AgregarEn(todas,c);
					}
					Tabla t=c.TablaContenedora;
					if(t!=null){
						if(TablasUsadas.IndexOf(t)<0){
							TablasUsadas.Add(t);
						}
					}
				}
			}
			{
				ParteSeparadora coma=new ParteSeparadora(", ");
				todas.Add(new LiteralSql(" FROM "));
				foreach(Tabla t in TablasUsadas){
					coma.AgregarEn(todas,t);
				}
			}
			todas.AddRange(PartesWhere());
			return todas;
		}
	}
	public class SentenciaInsert:SentenciaSelect{
		Tabla TablaBase;
		public SentenciaInsert(Tabla TablaBase){
			this.TablaBase=TablaBase;	
		}
		public SentenciaInsert Select(params Campo[] Campos){
			this.Campos.AddRange(Campos);
			return this;
		}
		public override PartesSql Partes(){
			PartesSql todas=new PartesSql();
			todas.Add(new LiteralSql("INSERT INTO "));
			todas.Add(TablaBase);
			ParteSeparadora coma=new ParteSeparadora(" (",", ");
			foreach(Campo c in Campos){
				coma.AgregarEn(todas,c);
			}
			todas.Add(new LiteralSql(") "));
			todas.AddRange(base.Partes());
			return todas;
		}
	}
	public class Ejecutador:TodoASql.EjecutadorSql{
		CamposSql CamposContexto=new CamposSql();
		public Ejecutador(BaseDatos db,params Tabla[] TablasContexto)
			:base(db)
		{
			foreach(Tabla t in TablasContexto){
				foreach(Campo c in t.CamposPk()){
					if(c.ValorSinTipo!=null){
						CamposContexto.Add(c);
					}
				}
			}
		}
		public void Ejecutar(Sentencia s){
			base.ExecuteNonQuery(Dump(s));
		}
		public string Dump(Sentencia laSentencia){
			Sentencia s=laSentencia;
			foreach(Tabla t in s.Tablas()){
				foreach(Campo c in CamposContexto){
					if(t.TieneElCampo(c)){
						s.Where(t.CampoIndirecto(c).Igual(c.ValorSinTipo));
					}
				}
			}
			StringBuilder rta=new StringBuilder();
			foreach(Sqlizable p in s.Partes()){
				rta.Append(p.ToSql(db));
			}
			rta.Append(";");
			return rta.ToString();
		}
	}
	public class ExpresionSql:Sqlizable{
		public PartesSql Partes=new PartesSql();
		public ExpresionSql(params Sqlizable[] Partes){
			this.Partes.AddRange(Partes);
		}
		ExpresionSql(PartesSql Partes){
			this.Partes=Partes;
		}
		public virtual ExpresionSql And(ExpresionSql otra){
			PartesSql nueva=new PartesSql();
			nueva.AddRange(Partes);
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
		public override string ToSql(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			foreach(Sqlizable s in Partes){
				rta.Append(s.ToSql(db));
			}
			return rta.ToString();
		}
		public class SelectSuma:Sqlizable{
			Tabla TablaBase;
			Campo CampoSumar;
			ExpresionSql ExpresionWhere;
			public SelectSuma(Tabla TablaBase,Campo CampoSumar,ExpresionSql ExpresionWhere){
				this.TablaBase=TablaBase;
				this.CampoSumar=CampoSumar;
				this.ExpresionWhere=ExpresionWhere;
			}
			public override string ToSql(BaseDatos db)
			{
				// return "";
				StringBuilder rta=new StringBuilder();
				if(db is BdAccess){
					rta.Append("DSum('"+db.StuffCampo(CampoSumar.NombreCampo)+"','"
					           +db.StuffTabla(TablaBase.NombreTabla)+"','");
					foreach(Sqlizable s in ExpresionWhere.Partes){
						if(s is Campo){
							Campo c=s as Campo;
							if(c.TablaContenedora!=TablaBase){
								rta.Append("''' & "+c.ToSql(db)+" & '''");
							}else{
								rta.Append(db.StuffCampo(c.NombreCampo));
							}
						}else{
							rta.Append(s.ToSql(db));
						}
					}
					rta.Append("')");
					return rta.ToString();
				}else{
					return "(SELECT sum("+CampoSumar.ToSql(db)+") FROM "+TablaBase.ToSql(db)+" WHERE "+ExpresionWhere.ToSql(db)+")";
					/*
					foreach(Sqlizable s in ExpresionWhere){
						rta.Append(s.ToSql(db));
					}
					*/
				}
				/* 
							    DSum('ponderador','grupos','grupopadre=''' & grupo & ''' and agrupacion=''' & agrupacion & '''')

							    (SELECT sum(h.ponderador)
							       FROM grupos h
							       WHERE h.grupopadre=grupos.grupo
							         AND h.agrupacion=grupos.agrupacion)
				 */ 
			}
			public static implicit operator ExpresionSql(SelectSuma ss){
				return new ExpresionSql(ss);
			}
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
		class Empresas:Tabla{
			[Pk] public CampoEntero cEmpresa;
			public CampoNombre cNombreEmpresa;
		}
		class Productos:Tabla{
			[Pk] public CampoEntero cEmpresa;
			[Pk] public CampoProducto cProducto;
			public CampoNombre cNombreProducto;
		}
		class PartesProductos:Tabla{
			[Pk] public CampoEntero cEmpresa;
			[Pk] public CampoProducto cProducto;
			[Pk] public CampoEntero cParte;
			public CampoNombre cNombreParte;
			public CampoEntero cCantidad;
		}
		[Test]
		public void Periodos(){
			Periodos p=new Periodos();
			Assert.AreEqual(0,p.cAno.Valor);
			Assert.AreEqual("Ano",p.cAno.Nombre);
			Assert.AreEqual("create table periodos(ano integer,mes integer,anoant integer,mesant integer,primary key(ano,mes));"
			                ,Cadena.Simplificar(p.SentenciaCreateTable()));
			Productos pr=new Productos();
			Assert.AreEqual("create table productos(empresa integer,producto varchar(4),nombreproducto varchar(250),primary key(empresa,producto));"
			                ,Cadena.Simplificar(pr.SentenciaCreateTable()));
		}
		[Test]
		public void SentenciaInsert(){
			Productos p=new Productos();
			BaseDatos dba=BdAccess.SinAbrir();
			Assert.AreEqual("INSERT INTO [productos] ([producto], [nombreproducto]) SELECT [producto], [producto] AS [nombreproducto] FROM [productos];",
				new Ejecutador(dba)
				.Dump(new SentenciaInsert(p).Select(p.cProducto,p.cNombreProducto.Es(p.cProducto))));
		}
		[Test]
		public void SentenciaUpdate(){
			Productos p=new Productos();
			BaseDatos dba=BdAccess.SinAbrir();
			Assert.AreEqual("UPDATE [productos] SET [producto]='P1', [nombreproducto]='Producto 1';",
			                new Ejecutador(dba)
			                .Dump(new SentenciaUpdate(p,p.cProducto.Set("P1"),p.cNombreProducto.Set("Producto 1"))));
			string Esperado="UPDATE [productos] SET [producto]='P1', [nombreproducto]='Producto 1' WHERE [producto]='P3' AND ([nombreproducto] IS NULL OR [nombreproducto]<>[producto])";
			Assert.AreEqual(Esperado+";",
			                new Ejecutador(dba)
			                .Dump(new SentenciaUpdate(p,p.cProducto.Set("P1"),p.cNombreProducto.Set("Producto 1"))
			                      .Where(p.cProducto.Igual("P3")
			                             .And(p.cNombreProducto.EsNulo()
			                                  .Or(p.cNombreProducto.Distinto(p.cProducto))))));
			SentenciaUpdate sentencia=new SentenciaUpdate(p,p.cProducto.Set("P1"),p.cNombreProducto.Set("Producto 1"));
			sentencia.Where(p.cProducto.Igual("P3")
			                .And(p.cNombreProducto.EsNulo()
			                     .Or(p.cNombreProducto.Distinto(p.cProducto))));
			Assert.AreEqual(1,sentencia.Tablas().Count);
			Assert.AreEqual("productos",sentencia.Tablas()[0].NombreTabla);
			sentencia.Where(p.cNombreProducto.Distinto("P0"));
			Esperado+=" AND [nombreproducto]<>'P0'";
			Assert.AreEqual(Esperado+";",new Ejecutador(dba).Dump(sentencia));
			Assert.AreEqual(Esperado+";",new Ejecutador(dba).Dump(sentencia));
			Empresas contexto=new Empresas();
			contexto.cEmpresa.AsignarValor(14);
			using(Ejecutador ej=new Ejecutador(dba,contexto)){
				Esperado+=" AND [empresa]=14";
				Assert.AreEqual(Esperado+";",ej.Dump(sentencia));
			}
		}	
		[Test]
		public void SentenciaCompuesta(){
			Productos p=new Productos();
			BaseDatos dba=BdAccess.SinAbrir();
			Empresas e=new Empresas();
			e.cEmpresa.Valor=13;
			using(Ejecutador ej=new Ejecutador(dba,e)){
				Sentencia s=
					new SentenciaSelect(e.cEmpresa,e.cNombreEmpresa)
					.Where(e.cEmpresa.Distinto(13));
				Assert.AreEqual("SELECT [empresa], [nombreempresa] FROM [empresas] WHERE [empresa]<>13;",
				                ej.Dump(s));
			}
		}
	}
}
