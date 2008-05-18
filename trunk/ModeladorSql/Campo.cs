/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 03/05/2008
 * Time: 12:39 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;

using Comunes;
using BasesDatos;

namespace ModeladorSql
{
	public class ListaCampos:ListaElementos<Campo>,IConCampos{
		public Lista<Campo> Campos(){
			return this;
		}
		public string ToSql(BaseDatos db){
			return this.ToSql(db);
		}
	}
	public abstract class Campo:IConCampos,IExpresion{
		public string Nombre;
		public string NombreCampo; 
		public bool EsPk;
		public bool Obligatorio;
		public Tabla TablaContenedora;
		public string DireccionOrderBy="";
		public abstract bool EsNumerico{ get; }
		public Lista<Campo> Campos(){
			return new Lista<Campo>(this);
		}
		public abstract string TipoCampo{ get; }
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
		public virtual string Opcionalidad{ 
			get{ if(Obligatorio){ return " NOT NULL"; }else{ return ""; } }
		}
		public abstract string DefinicionPorDefecto(BaseDatos db);
		public virtual string ToSql(BaseDatos db,bool ForzarAs){
			string alias="";
			if(TablaContenedora!=null /* || this.TablaContenedora.Alias==null*/){
				alias=TablaContenedora.AliasActual??TablaContenedora.NombreTabla;
			}
			return alias+(alias==""?"":".")+db.StuffCampo(NombreCampo)+(ForzarAs?" AS "+db.StuffCampo(NombreCampo):"");
		}
		public virtual string ToSql(BaseDatos db){
			return ToSql(db,false);
		}
		public override string ToString(){
			return 
				"«"+
				(TablaContenedora!=null?(
					TablaContenedora.Alias!=null?TablaContenedora.Alias:TablaContenedora.NombreTabla
				)+".":"")+
				Nombre+
				(ValorSinTipo!=null?"="+ValorSinTipo.ToString():"")+"»";
		}
		public virtual bool CandidatoAGroupBy{ 
			get{
				return true;
			}
		}
		public virtual ConjuntoTablas Tablas(QueTablas queTablas){
			if(TablaContenedora==null){
				return new ConjuntoTablas();
			}else{
				// return new ConjuntoTablas(TablaContenedora);
				return TablaContenedora.Tablas(queTablas);
			}
		}
		public Campo Desc(){
			this.DireccionOrderBy=" DESC";
			return this;
		}
		public abstract bool EsAgrupada{ get; }
		public CampoAlias<object> EsObject(IExpresion expresion){
			return new CampoAlias<object>(this,expresion);
		}
		public virtual IExpresion Expresion{
			get{ return this; }
		}
		public int Precedencia{
			get{ return 9; }
		}
		public int PrecedenciaOperador{
			get{ return 9; }
		}
	}
	public interface ICampoAlias:IElemento{
		IExpresion ExpresionBase{ get; }
		Campo CampoReceptor{ get; }
	}
	public class CampoAlias<T>:CampoTipo<T>,ICampoAlias{
		Campo campoReceptor; public Campo CampoReceptor{ get{ return campoReceptor; } }
		IExpresion expresionBase; public IExpresion ExpresionBase{ get{ return expresionBase; } }
		public CampoAlias(Campo CampoReceptor,IExpresion ExpresionBase){
			this.NombreCampo=CampoReceptor.NombreCampo;
			this.Nombre=CampoReceptor.Nombre;
			// this.TablaContenedora=CampoDestino.TablaContenedora;
			this.campoReceptor=CampoReceptor;
			this.expresionBase=ExpresionBase;
			System.Reflection.FieldInfo f=CampoReceptor.GetType().GetField("CampoContenedor");
			if(f!=null){
				f.SetValue(CampoReceptor,this);
			}
		}
		/*
		public CampoAlias(Campo CampoReceptor,bool EsAgrupada,params Sqlizable[] Partes)
			:this(CampoReceptor,EsAgrupada,new ExpresionSql(Partes)){
		}
		public CampoAlias(Campo CampoReceptor,bool EsAgrupada,ListaSqlizable<Sqlizable> Partes)
			:this(CampoReceptor,EsAgrupada,new ExpresionSql(Partes)){
		}
		public CampoAlias(Campo CampoReceptor,ExpresionSql ExpresionBase)
			:this(CampoReceptor,ExpresionBase.TipoAgrupada,ExpresionBase){
		}
		*/
		public override object ValorSinTipo {
			get { 
				Falla.Detener("Un campo base no tiene Valor propio");
				return null; 
			}
		}
		public override void AsignarValor(object valor){
			Falla.Detener("Un campo base no tiene Valor propio (no se le puede asignar)");
		}
		public override string ToSql(BaseDatos db,bool ForzarAs){
			return ToSql(db);
		}
		public override string ToSql(BaseDatos db){
			return ExpresionBase.ToSql(db)+" AS "+db.StuffCampo(CampoReceptor.NombreCampo);
		}
		public override string DefinicionPorDefecto(BaseDatos db){
			return "";
		}
		public override string TipoCampo {
			get { 
				Falla.Detener("Un campo base no tiene Tipo propio (no se puede definir tabla)");
				return "";
			}
		}
		public override bool CandidatoAGroupBy{ 
			get{
				return false;
			}
		}
		public override bool EsAgrupada {
			get { return ExpresionBase.EsAgrupada; }
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			ConjuntoTablas rta=new ConjuntoTablas();
			rta.AddRange(ExpresionBase.Tablas(queTablas));
			return rta;
		}
		public override IExpresion Expresion{
			get{ return ExpresionBase; }
		}
	}
	public class CampoTipo<T>:Campo,IElementoTipado<T>{
		protected T valor;
		protected bool contieneNull;
		public object ValorPorDefecto;
		public virtual T Valor{ get{ return valor;} set{ valor=value; } }
		public override object ValorSinTipo{ 
			get{ 
				if(contieneNull){
					return null;
				}
				return valor;
			}
		}
		public virtual bool ContieneNull{ get{ return contieneNull; }}
		string TipoCampoS(Type tipo){
			if(tipo==typeof(int)){
				return "INTEGER";
			}else if(tipo==typeof(string)){
				return "VARCHAR";
	         }else if(tipo==typeof(double)){
				return "DOUBLE PRECISION";
			}else if(tipo.IsGenericType){
				return TipoCampoS(tipo.GetGenericArguments()[0]);
			}else{
				return tipo.Name; 
			}
		}
		public override bool EsNumerico{ 
			get{ 
				return TipoCampo.StartsWith("INTEGER") || TipoCampo.StartsWith("DOUBLE");
			}
		}
		public override bool EsAgrupada{ 
			get{ return false; }
		}
		public string prueba<T2,T3>(){
			return typeof(T2).FullName+typeof(T3).FullName;
		}
		public CampoAlias<T> Es(IElementoTipado<T> expresion){
			return new CampoAlias<T>(this,expresion);
		}
		public CampoAlias<T> Es(ElementoTipado<T> expresion){
			return new CampoAlias<T>(this,expresion);
		}
		public CampoAlias<T> Es(CampoTipo<T> expresion){
			return new CampoAlias<T>(this,expresion);
		}
		public CampoAlias<object> Es(Campo expresion){
			return new CampoAlias<object>(this,expresion);
		}
		public virtual ElementoLogico EsNulo(){
			return new OperacionSufijaLogica<T>(this,OperadorSufijoLogico.EsNulo);
		}
		public virtual ElementoLogico NoEsNulo(){
			return new OperacionSufijaLogica<T>(this,OperadorSufijoLogico.NoEsNulo);
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
				valor=null;
			}else if(typeof(T)==typeof(bool) && valor is string){
				bool valorBool=((string)valor=="S");
				valor=valorBool;
			}else if(typeof(T).IsEnum && valor is string){
				valor=Enum.Parse(typeof(T),(string)valor);
			}
			if(valor==null){
				if(ValorPorDefecto!=null){
					valor=(T)ValorPorDefecto;
				}else{
					valor=null;
				}
			}
			if(valor==null){
				contieneNull=true;
				this.valor=default(T);
			}else{
				contieneNull=false;
				this.valor=(T)valor;
			}
		}
		public override string DefinicionPorDefecto(BaseDatos db){
			if(ValorPorDefecto!=null){
				return " default "+db.StuffValor(ValorPorDefecto);
			}else{
				return "";
			}
		}
		/*
		public virtual CampoAlias Es(IExpresion expresion){
			return new CampoAlias(this, expresion);
		}
		*/
		public virtual CampoAlias<T> SeaNulo(){
			return Es(Constante<T>.Nula);
		}
		/*
		public CampoAlias EsExpresionAgrupada(string operador,ExpresionSql expresion,string subOperador,string postOperador){
			ListaSqlizable<Sqlizable> nueva=new ListaSqlizable<Sqlizable>();
			if(subOperador.Contains("LOG")){
				int pos=subOperador.IndexOf("LOG");
				nueva.Add(new LiteralSql(operador+"("+subOperador.Substring(0,pos)));
				nueva.Add(new FuncionLn());
				nueva.Add(new LiteralSql(subOperador.Substring(pos+3)));
			}else{
				nueva.Add(new LiteralSql(operador+"("+subOperador));
			}
			nueva.AddRange(expresion.Partes);
			nueva.Add(new LiteralSql(postOperador+")"));
			return new CampoAlias(this,true,nueva);
		}
		public CampoAlias EsExpresionAgrupada(string operador,ExpresionSql expresion){
			return EsExpresionAgrupada(operador,expresion,"","");
		}
		*/
		public ElementoLogico Igual(ElementoTipado<T> expresion){
			return new BinomioRelacional<T>(this,OperadorBinarioRelacional.Igual,expresion);
		}
		public ElementoLogico Igual(IElementoTipado<T> expresion){
			return new BinomioRelacional<T>(this,OperadorBinarioRelacional.Igual,expresion);
		}
		/*
		public ElementoTipado<bool> Mayor(IElementoTipado<T> expresion){
			return new BinomioRelacional<T>(this,OperadorBinarioRelacional.Mayor,expresion);
		}
		*/
		public ElementoLogico Distinto(ElementoTipado<T> expresion){
			return new BinomioRelacional<T>(this,OperadorBinarioRelacional.Distinto,expresion);
		}
		public ElementoLogico Distinto(IElementoTipado<T> expresion){
			return new BinomioRelacional<T>(this,OperadorBinarioRelacional.Distinto,expresion);
		}
		public CampoTipo<T> EsMax(IElementoTipado<T> expresion){
			return Es(new FuncionAgrupacion<T,T>(expresion,OperadorAgrupada.Maximo));
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
	public interface ICampoDestino{
	}
	public class CampoDestino<T>:CampoNumericoTipo<T>,ICampoDestino{
		public CampoAlias<T> CampoContenedor;
		public int OrdenDeReemplazo;
		public CampoDestino(string NombreCampo){
			this.NombreCampo=NombreCampo;
			Archivo.Escribir("tmp_aca.txt",NombreCampo+": "+Objeto.ExpandirTodo(this.GetType()));
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas)
		{
			return CampoContenedor.Tablas(queTablas);
		}
		public override IExpresion Expresion {
			get { return CampoContenedor.Expresion; }
		}
	}
	public class CampoNumericoTipo<T>:CampoTipo<T>,IElementoNumerico<T>{
		public Campo EsSuma<T2>(IElementoNumerico<T2> expresion){
			return Es(new FuncionAgrupacion<T2,T>(expresion,OperadorAgrupada.Suma));
		}
		public Campo EsPromedioGeometrico(IElementoTipado<T> expresion){
			return Es(new FuncionAgrupacion<T,T>(expresion,OperadorAgrupada.PromedioGeometrico));
		}
		public IElementoTipado<T> Mas(ElementoTipado<T> Valor){
			return new Binomio<T>{E1=this,Operador=OperadorBinario.Mas,E2=Valor};
		}
		public IElementoTipado<T> Por(IElementoTipado<T> Valor){
			return new Binomio<T>{E1=this,Operador=OperadorBinario.Por,E2=Valor};
		}
		public ElementoTipado<string> NumeroACadena(){
			return new OperacionFuncion<T,string>(this, OperadorFuncion.Str);
		}
		public CampoAlias<int> Es(IElementoTipado<int> expresion){
			return new CampoAlias<int>(this,expresion);
		}
	}
	public class CampoEntero:CampoNumericoTipo<int>{
		public CampoEntero(){
			Obligatorio=true;
		}
	}
	public class CampoEnteroOpcional:CampoNumericoTipo<int>,IContenidoOpcional{
	}
	public class CampoChar:CampoTipo<string>{
		public int Largo;
		protected CampoChar(int largo){
			this.Largo=largo;	
		}
		public override string TipoCampo{ 
			get { return "VARCHAR("+Largo.ToString()+")"; }
		}
		public ElementoTipado<string> Concatenado(string constante){
			return new Binomio3T<string,string,string>{E1=this,Operador=OperadorBinario.Concatenado,E2=new Constante<string>(constante)};
		}
		public ElementoTipado<string> Concatenado<T>(IElementoTipado<T> expresion){
			return new Binomio3T<string,T,string>{E1=this,Operador=OperadorBinario.Concatenado,E2=expresion};
		}
		public ElementoTipado<string> Concatenado<T>(ElementoTipado<T> expresion){
			return new Binomio3T<string,T,string>{E1=this,Operador=OperadorBinario.Concatenado,E2=expresion};
		}
	}
	public class CampoReal:CampoNumericoTipo<double>{
		public CampoReal(){ Obligatorio=true; }
	}
	public class CampoRealOpcional:CampoNumericoTipo<double>,IContenidoOpcional{};
	public class CampoLogico:CampoTipo<bool>{
		public CampoLogico(){
			ValorPorDefecto=false;
		}
		public override string TipoCampo {
			get { return "varchar(1)"; }
		}
	}
	public class CampoLogicoTriestado:CampoLogico,IContenidoOpcional{
	}
	public abstract class AplicadorCampo:System.Attribute{
	   	public abstract void Aplicar(ref Campo campo);
	}
	public class Pk:AplicadorCampo{
	   	public override void Aplicar(ref Campo campo){
	   		campo.EsPk=true;
	    }
	}
	public class Uk:AplicadorCampo{
	   	public override void Aplicar(ref Campo campo){
	    }
	}
}
