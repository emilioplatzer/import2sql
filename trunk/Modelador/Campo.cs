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

using Comunes;
using BasesDatos;
using Modelador;

namespace Modelador
{
	public abstract class Campo:Campable{
		public string Nombre;
		public string NombreCampo;
		public abstract string TipoCampo{ get; }
		public bool EsPk;
		public bool Obligatorio;
		public Tabla TablaContenedora;
		public string DireccionOrderBy;
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
		public virtual ExpresionSql Expresion{
			get{
				return new ExpresionSql(this);
			}
		}
		/*
		public void Entablar(Tabla tabla){ // marcarlo como perteneciente a la tabla
			TablaContenedora=tabla;
		}
		*/
		public virtual ExpresionSql EsNulo(){
			return new ExpresionSql(this,new LiteralSql(" IS NULL"));
		}
		public virtual ExpresionSql NoEsNulo(){
			return new ExpresionSql(this,new LiteralSql(" IS NOT NULL"));
		}
		public virtual string Opcionalidad{ 
			get{ if(Obligatorio){ return " not null"; }else{ return ""; } }
		}
		public abstract string DefinicionPorDefecto(BaseDatos db);
		public override string ToSql(BaseDatos db)
		{
			if(this.TablaContenedora==null || this.TablaContenedora.Alias==null){
				return db.StuffCampo(this.NombreCampo);
			}else{
				return this.TablaContenedora.Alias+"."+db.StuffCampo(NombreCampo);
			}
		}
		public override bool CandidatoAGroupBy{ 
			get{
				return true;
			}
		}
		public ExpresionSql Operado<T>(string OperadorTextual,T expresion){
			return new ExpresionSql(this.Expresion,new LiteralSql(OperadorTextual),new ValorSql<T>(expresion));
		}
		public ExpresionSql Igual<T>(T expresion){
			return Operado<T>("=",expresion);
		}
		public ExpresionSql MayorOIgual<T>(T expresion){
			return Operado<T>(">=",expresion);
		}
		public ExpresionSql Distinto<T>(T expresion){
			return Operado<T>("<>",expresion);
		}
		public CampoAlias Es(ExpresionSql expresion){
			return new CampoAlias(this,expresion);
		}
		public CampoAlias Es(Campo campo){
			return Es(new ExpresionSql(campo));
		}
		public override ListaSqlizable<Campo> Campos(){
			ListaSqlizable<Campo> rta=new ListaSqlizable<Campo>();
			rta.Add(this);
			return rta;
		}
		public override ConjuntoTablas Tablas(){
			ConjuntoTablas rta=new ConjuntoTablas();
			if(TablaContenedora!=null){
				rta.Add(TablaContenedora);
			}
			return rta;
		}
		public Campo Desc(){
			this.DireccionOrderBy=" DESC";
			return this;
		}
	}
	public class CampoAlias:Campo{
		public Campo CampoReceptor;
		public ExpresionSql ExpresionBase;
		public CampoAlias(Campo CampoReceptor,bool EsAgrupada,ExpresionSql ExpresionBase){
			this.NombreCampo=CampoReceptor.NombreCampo;
			this.Nombre=CampoReceptor.Nombre;
			// this.TablaContenedora=CampoDestino.TablaContenedora;
			this.CampoReceptor=CampoReceptor;
			this.ExpresionBase=ExpresionBase;
			this.ExpresionBase.TipoAgrupada=EsAgrupada;
			FieldInfo f=CampoReceptor.GetType().GetField("CampoContenedor");
			if(f!=null){
				f.SetValue(CampoReceptor,this);
			}
		}
		public CampoAlias(Campo CampoReceptor,bool EsAgrupada,params Sqlizable[] Partes)
			:this(CampoReceptor,EsAgrupada,new ExpresionSql(Partes)){
		}
		public CampoAlias(Campo CampoReceptor,bool EsAgrupada,ListaSqlizable<Sqlizable> Partes)
			:this(CampoReceptor,EsAgrupada,new ExpresionSql(Partes)){
		}
		public CampoAlias(Campo CampoReceptor,ExpresionSql ExpresionBase)
			:this(CampoReceptor,ExpresionBase.TipoAgrupada,ExpresionBase){
		}
		public override object ValorSinTipo {
			get { 
				Assert.Fail("Un campo base no tiene Valor propio");
				return null; 
			}
		}
		public override void AsignarValor(object valor)
		{
			Assert.Fail("Un campo base no tiene Valor propio (no se le puede asignar)");
		}
		public override string ToSql(BaseDatos db)
		{
			return ExpresionBase.ToSql(db)/*+" AS "+CampoDestino.ToSql(db)*/;
		}
		public override string DefinicionPorDefecto(BaseDatos db)
		{
			return "";
		}
		public override string TipoCampo {
			get { 
				Assert.Fail("Un campo base no tiene Tipo propio (no se puede definir tabla)");
				return "";
			}
		}
		public override bool CandidatoAGroupBy{ 
			get{
				return false;
			}
		}
		public override ExpresionSql Expresion{
			get{
				return ExpresionBase;
			}
		}
		public override ConjuntoTablas Tablas(){
			ConjuntoTablas rta=new ConjuntoTablas();
			rta.AddRange(ExpresionBase.Tablas());
			return rta;
		}
	}
	public class CampoTipo<T>:Campo{
		protected T valor;
		public object ValorPorDefecto;
		public virtual T Valor{ get{ return valor;} set{ valor=value; } }
		public override object ValorSinTipo{ get{ return valor;} }
		string TipoCampoS(Type tipo){
			if(tipo==typeof(int)){
				return "integer";
			}else if(tipo==typeof(string)){
				return "varchar";
	         }else if(tipo==typeof(double)){
				return "double precision";
			}else if(tipo.IsGenericType){
				return TipoCampoS(tipo.GetGenericArguments()[0]);
			}else{
				return tipo.Name; 
			}
		}
		public override string TipoCampo{ 
			get {
				return TipoCampoS(typeof(T));
			} 
		}
		public CampoTipo()
		{	
		}
		public override void AsignarValor(object valor){
			if(valor is DBNull){
				if(ValorPorDefecto!=null){
					valor=(T)ValorPorDefecto;
				}else{
					valor=null;
				}
			}else if(typeof(T)==typeof(bool) && valor is string){
				bool valorBool=((string)valor=="S");
				valor=valorBool;
			}else if(typeof(T).IsEnum && valor is string){
				valor=Enum.Parse(typeof(T),(string)valor);
			}
			this.valor=(T)valor;
		}
		public override string DefinicionPorDefecto(BaseDatos db){
			if(ValorPorDefecto!=null){
				return " default "+db.StuffValor(ValorPorDefecto);
			}else{
				return "";
			}
		}
		public virtual SentenciaUpdate.Sets Set(T valor){
			return new SentenciaUpdate.Sets(this,new ExpresionSql(new ValorSql<T>(valor)));
		}
		public virtual SentenciaUpdate.Sets Set(ExpresionSql expresion){
			return new SentenciaUpdate.Sets(this,expresion);
		}
		public virtual SentenciaUpdate.Sets Set(Campo campo){
			return new SentenciaUpdate.Sets(this,new ExpresionSql(campo));
		}
		public virtual SentenciaUpdate.Sets SetNull(){
			return new SentenciaUpdate.Sets(this,new ExpresionSql(new ValorSqlNulo()));
		}
		public CampoAlias Es(T valor){
			return Es(new ExpresionSql(new ValorSql<T>(valor)));
		}
		public CampoAlias EsExpresionAgrupada(string operador,ExpresionSql expresion,string subOperador,string postOperador){
			ListaSqlizable<Sqlizable> nueva=new ListaSqlizable<Sqlizable>();
			nueva.Add(new LiteralSql(operador+"("+subOperador));
			nueva.AddRange(expresion.Partes);
			nueva.Add(new LiteralSql(postOperador+")"));
			return new CampoAlias(this,true,nueva);
		}
		public CampoAlias EsExpresionAgrupada(string operador,ExpresionSql expresion){
			return EsExpresionAgrupada(operador,expresion,"","");
		}
		public Campo EsMax(ExpresionSql expresion){
			return EsExpresionAgrupada("MAX",expresion);
		}
		public Campo EsMax(Campo campo){
			return EsExpresionAgrupada("MAX",new ExpresionSql(campo));
		}
		public Campo EsCount(){
			return new CampoAlias(this,true,new ExpresionSql(new LiteralSql("COUNT(*)")));
		}
		public Campo EsCount(ExpresionSql expresion){
			return EsExpresionAgrupada("COUNT",expresion);
		}
		public Campo EsCount(Campo campo){
			return EsExpresionAgrupada("COUNT",new ExpresionSql(campo));
		}
		#if NuncaAccess
		// habilitar esto si nunca se va a trabajar en Access
		public Campo EsCountDistinct(ExpresionSql expresion){
			return EsExpresionAgrupada("COUNT",expresion,"DISTINCT ");
		}
		public Campo EsCountDistinct(Campo campo){
			return EsExpresionAgrupada("COUNT",new ExpresionSql(campo),"DISTINCT ");
		}
		#endif
	}
	public class CampoPkTipo<T>:CampoTipo<T>{
		public CampoPkTipo()
		{	
			EsPk=true;
		}
	}
	public class CampoEnumerado<T>:CampoTipo<T>{
		public int LongitudDefinicion;
		public int MaximaLongitud;
		public CampoEnumerado(){
			string[] nombres=Enum.GetNames(typeof(T));
			foreach(string nombre in nombres){
				if(nombre.Length>MaximaLongitud){
					MaximaLongitud=nombre.Length;
				}
			}
			LongitudDefinicion=MaximaLongitud*2;
			if(LongitudDefinicion<10){
				LongitudDefinicion=10;
			}
		}
		public override string TipoCampo {
			get { return "varchar("+LongitudDefinicion+")"; }
		}
	}
	public class CampoDestino<T>:CampoNumericoTipo<T>{
		public CampoAlias CampoContenedor;
		public CampoDestino(string NombreCampo){
			this.NombreCampo=NombreCampo;
			Archivo.Escribir("tmp_aca.txt",NombreCampo+": "+Objeto.ExpandirTodo(this.GetType()));
			// System.Windows.Forms.MessageBox.Show("Campo destino "+NombreCampo+": "+Objeto.ExpandirTodo(this.GetType()));
		}
		public override ExpresionSql Expresion {
			get { 
				if(CampoContenedor!=null){
					return CampoContenedor.Expresion; 
				}else{
					return new ExpresionSql(new ValorSql<object>(ValorSinTipo));
				}
			}
		}
	}
	public class CampoNumericoTipo<T>:CampoTipo<T>{
		public Campo EsSuma(ExpresionSql expresion){
			return EsExpresionAgrupada("SUM",expresion);
		}
		public Campo EsSuma(Campo campo){
			return EsExpresionAgrupada("SUM",new ExpresionSql(campo));
		}
		public Campo EsPromedioGeometrico(ExpresionSql expresion){
			return EsExpresionAgrupada("EXP",expresion,"AVG(LN","))");
		}
		public Campo EsPromedioGeometrico(Campo campo){
			return EsExpresionAgrupada("SUM",new ExpresionSql(campo),"AVG(LN","))");
		}
		public ExpresionSql Mas<T2>(T2 Valor){
			return Operado<T2>("+",Valor);
		}
		public ExpresionSql Por<T2>(T2 Valor){
			return Operado<T2>("*",Valor);
		}
		public ExpresionSql Dividido<T2>(T2 Valor){
			return Operado<T2>("/",Valor);
		}
	}
	public class CampoEntero:CampoNumericoTipo<int>{
		public CampoEntero(){
			Obligatorio=true;
		}
	};
	public class CampoEnteroOpcional:CampoNumericoTipo<int?>{
	};
	public class CampoChar:CampoTipo<string>{
		public int Largo;
		protected CampoChar(int largo){
			this.Largo=largo;	
		}
		public override string TipoCampo{ 
			get { return "varchar("+Largo.ToString()+")"; }
		}
		public ExpresionSql Concatenado<T>(T expresion){
			return new ExpresionSql(new OperadorConcatenacionIzquierda()
			                        ,this,new OperadorConcatenacionMedio()
			                        ,new ValorSql<T>(expresion),new OperadorConcatenacionDerecha());
		}
	};
	public class CampoReal:CampoNumericoTipo<double>{
		public CampoReal(){ Obligatorio=true; }
	};
	public class CampoRealOpcional:CampoNumericoTipo<double?>{};
	public class CampoLogicoTriestado:CampoTipo<bool>{
		public override string TipoCampo {
			get { return "varchar(1)"; }
		}
	}
	public class CampoLogico:CampoLogicoTriestado{
		public CampoLogico(){
			Obligatorio=true;
			ValorPorDefecto=false;
		}
	}
	public abstract class AplicadorCampo:System.Attribute{
	   	public abstract void Aplicar(ref Campo campo);
	}
	public class Pk:AplicadorCampo{
	   	public override void Aplicar(ref Campo campo){
	   		campo.EsPk=true;
	    }
	}
}
