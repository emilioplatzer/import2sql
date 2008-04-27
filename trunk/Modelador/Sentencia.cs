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
	public class Diccionario<TKey, TValue>:System.Collections.Generic.Dictionary<TKey, TValue>{};
	public class Lista<T>:System.Collections.Generic.List<T>{};
	public class ConjuntoTablas:Conjunto<Tabla>{};
	/*
	public class ConjuntoTablas:System.Collections.Generic.Dictionary<Tabla, int>{
		ConjuntoTablas AddAdd(Tabla t,int cuanto){
			if(this.ContainsKey(t)){
				this[t]+=cuanto;
			}else{
				this.Add(t,cuanto);
			}
			return this;
		}
		public ConjuntoTablas Add(Tabla t){
			return AddAdd(t,1);
		}
		public ConjuntoTablas AddRange(ConjuntoTablas conj){
			foreach(System.Collections.Generic.KeyValuePair<Tabla, int> t in conj){
				this.AddAdd(t.Key,t.Value);
			}
			return this;
		}
		public bool Contains(Tabla t){
			return ContainsKey(t);
		}
	}
	*/
	public class ListaSqlizable<TSqlizable>:Lista<TSqlizable> where TSqlizable : Sqlizable{
		public ListaSqlizable(){}
		public ListaSqlizable(params TSqlizable[] elementos){
			foreach(TSqlizable s in elementos){
				this.Add(s);
			}
		}
		public ConjuntoTablas Tablas(){
			ConjuntoTablas rta=new ConjuntoTablas();
			foreach(TSqlizable s in this){
				rta.AddRange(s.Tablas());
			}
			return rta;
		}
	}
	public abstract class Sentencia{
		ListaSqlizable<Sqlizable> ParteWhere=new ListaSqlizable<Sqlizable>();
		ListaSqlizable<Sqlizable> ParteHaving=new ListaSqlizable<Sqlizable>();
		protected System.Collections.Generic.Dictionary<string, string> AliasTablas=new System.Collections.Generic.Dictionary<string, string>();
		protected virtual void ClonarMiembros(Sentencia s){
			ParteWhere=new ListaSqlizable<Sqlizable>();
			ParteWhere.AddRange(s.ParteWhere);
			ParteHaving=new ListaSqlizable<Sqlizable>();
			ParteHaving.AddRange(s.ParteHaving);
			AliasTablas=new System.Collections.Generic.Dictionary<string, string>(s.AliasTablas);
			/*
			if(s.Tablas()Usadas==null){
				TablasUsadas=null;
			}else{
				TablasUsadas=new Lista<Tabla>();
				TablasUsadas.AddRange(s.TablasUsadas);
			}
			*/
		}
		public abstract ListaSqlizable<Sqlizable> Partes();
		public abstract ConjuntoTablas Tablas();
		Sentencia AgregarParte(ListaSqlizable<Sqlizable> ListaPartes,params ExpresionSql[]expresiones){
			foreach(ExpresionSql expresion in expresiones){
				if(ListaPartes.Count>0){
					ListaPartes.Add(new LiteralSql("\n AND "));
				}
				ListaPartes.Add(expresion);
			}
			return this;
		}
		public Sentencia Where(params ExpresionSql[] expresiones){
			return AgregarParte(ParteWhere,expresiones);
		}
		public Sentencia Having(params ExpresionSql[] expresiones){
			return AgregarParte(ParteHaving,expresiones);
		}
		public ListaSqlizable<Sqlizable> DevolverParte(string clausula,ListaSqlizable<Sqlizable> ListaPartes){
			ListaSqlizable<Sqlizable> rta=new ListaSqlizable<Sqlizable>();
			if(ListaPartes.Count>0){
				rta.Add(new LiteralSql("\n "+clausula+" "));
				rta.AddRange(ListaPartes);
			}
			return rta;
		}
		public ListaSqlizable<Sqlizable> PartesWhere(){
			return DevolverParte("WHERE",ParteWhere);
		}
		public ListaSqlizable<Sqlizable> PartesHaving(){
			return DevolverParte("HAVING",ParteHaving);
		}
		/*
		protected void RegistrarTablas(Campo c){
			ExpresionSql expresion=c.ExpresionBase;
			if(expresion!=null){
				RegistrarTablas(expresion);
			}else{
				RegistrarTabla(c.TablaContenedora);
			}
		}
		protected void RegistrarTablas(ListaSqlizable<Campo> Campos){
			foreach(Campo c in Campos){
				RegistrarTablas(c);
			}
		}
		protected void RegistrarTablas(ExpresionSql expresion){
			RegistrarTablas(expresion.Partes);
		}
		protected void RegistrarTablas(ListaSqlizable<Sqlizable> Partes){
			foreach(Sqlizable s in Partes){
				if(s is Campo){
					RegistrarTablas(s as Campo);
				}else if(s is ExpresionSql){
					RegistrarTablas(s as ExpresionSql);
				}else if(s is ValorSql<Campo>){
					RegistrarTablas((s as ValorSql<Campo>).Valor);
				}else if(s is ValorSql<ExpresionSql>){
					RegistrarTablas((s as ValorSql<ExpresionSql>).Valor);
				}else if(s is SentenciaUpdate.Sets){
					RegistrarTablas((s as SentenciaUpdate.Sets).ValorAsignar);
				}else if(s is ExpresionSql.SelectSuma){
					AsignarAlias((s as ExpresionSql.SelectSuma).CampoSumar.TablaContenedora);
				}
			}
		}
		*/
		public void AsignarAlias(Tabla t){
			int Largo=1;
			int Sufijo=0;
			string Alias=t.NombreTabla.Substring(0,Largo);
			while(AliasTablas.ContainsKey(Alias)){
				if(Sufijo==0 && Largo<t.NombreTabla.Length){
					Largo++;
				}else{
					Largo=1;
					Sufijo++;
				}
				if(Sufijo==0){
					Alias=t.NombreTabla.Substring(0,Largo);
				}else{
					Alias=t.NombreTabla.Substring(0,Largo)+Sufijo.ToString();
				}
			}
			AliasTablas.Add(Alias,t.NombreTabla);
			t.Alias=Alias;
		}
		public void AsignarAlias(){
			AliasTablas=new System.Collections.Generic.Dictionary<string, string>();
			foreach(Tabla t in Tablas().Keys){
				AsignarAlias(t);
			}
		}
		/*
		protected void RegistrarTabla(Tabla t){
			if(t!=null){
				if(TablasUsadas.IndexOf(t)<0){
					TablasUsadas.Add(t);
					AsignarAlias(t);
				}
			}
		}
		*/
		public virtual Sentencia Clonate(){
			Sentencia rta=(Sentencia) this.MemberwiseClone();
			rta.ClonarMiembros(this);
			return rta;
		}
	}
	public class SentenciaUpdate:Sentencia{
		public Tabla TablaBase;
		public ListaSqlizable<Sqlizable> ParteSet=new ListaSqlizable<Sqlizable>();
		public SentenciaUpdate(Tabla tabla,Sets primerSet,params Sets[] sets){
			TablaBase=tabla;
			ParteSet.Add(primerSet);
			ParteSet.AddRange(sets);
			/*
			foreach(Sets s in sets){
				ParteSet.Add(s);
			}
			*/
		}
		public class Sets:Sqlizable{
			public Campo CampoAsignado;
			public ExpresionSql ValorAsignar;
			public Sets(Campo CampoAsignado,ExpresionSql ValorAsignar){
				this.CampoAsignado=CampoAsignado;
				this.ValorAsignar=ValorAsignar;
			}
			public override string ToSql(BaseDatos db)
			{
				return CampoAsignado.ToSql(db)+"="+ValorAsignar.ToSql(db);
			}
			public override bool CandidatoAGroupBy{ 
				get{
					Assert.Fail("No deber�a preguntar si un Set tiene variables");
					return false;
				}
			}
			public override ConjuntoTablas Tablas(){
				return ValorAsignar.Tablas();
			}
		}
		public override ListaSqlizable<Sqlizable> Partes(){
			ListaSqlizable<Sqlizable> todas=new ListaSqlizable<Sqlizable>();
			todas.AddRange(ParteSet);
			todas.AddRange(PartesWhere());
			return todas;
		}
		public override ConjuntoTablas Tablas(){
			ConjuntoTablas TablasUsadas=new ConjuntoTablas();
			TablasUsadas.Add(TablaBase);
			TablasUsadas.AddRange(ParteSet.Tablas());
			TablasUsadas.AddRange(PartesWhere().Tablas());
			TablasUsadas.AddRange(PartesHaving().Tablas());
			return TablasUsadas;
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
		public void AgregarEn(ListaSqlizable<Sqlizable> Partes,params Sqlizable[] Parte){
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
		protected ListaSqlizable<Campo> Campos=new ListaSqlizable<Campo>();
		public SentenciaSelect(params Campo[] Campos){
			this.Campos.AddRange(Campos);
		}
		public override ListaSqlizable<Sqlizable> Partes(){
			ListaSqlizable<Sqlizable> todas=new ListaSqlizable<Sqlizable>();
			ListaSqlizable<Sqlizable> groupBy=new ListaSqlizable<Sqlizable>();
			bool tieneAgrupados=false;
			todas.Add(new LiteralSql("SELECT "));
			{
				ParteSeparadora coma=new ParteSeparadora(", ");
				ParteSeparadora sepGB=new ParteSeparadora("\n GROUP BY ",", ");
				foreach(Campo c in Campos){
					if(c is CampoAlias){
						ExpresionSql expresion=(c as CampoAlias).ExpresionBase;
						if(expresion.TipoAgrupada){
							tieneAgrupados=true;
						}else if(expresion.CandidatoAGroupBy){
							sepGB.AgregarEn(groupBy,expresion);
						}
						coma.AgregarEn(todas,expresion,new LiteralSql(" AS "),new CampoReceptorInsert(c));
					}else{
						sepGB.AgregarEn(groupBy,c);
						coma.AgregarEn(todas,c);
					}
				}
			}
			{
				ParteSeparadora coma=new ParteSeparadora(", ");
				todas.Add(new LiteralSql("\n FROM "));
				foreach(Tabla t in Tablas().Keys){
					coma.AgregarEn(todas,t);
				}
			}
			todas.AddRange(PartesWhere());
			if(tieneAgrupados){
				todas.AddRange(groupBy);
				todas.AddRange(PartesHaving());
			}
			return todas;
		}
		public override ConjuntoTablas Tablas(){
			ConjuntoTablas rta=new ConjuntoTablas();
			rta.AddRange(Campos.Tablas());
			rta.AddRange(PartesWhere().Tablas());
			return rta;
		}
	}
	public class CampoReceptorInsert:Sqlizable{
		Campo CampoSinAlias;
		public CampoReceptorInsert(Campo CampoSinAlias){
			this.CampoSinAlias=CampoSinAlias;
		}
		public override string ToSql(BaseDatos db)
		{
			return db.StuffCampo(CampoSinAlias.NombreCampo);
		}
		public override bool CandidatoAGroupBy{ get{return CampoSinAlias.CandidatoAGroupBy;} }
		public override ConjuntoTablas Tablas(){
			return CampoSinAlias.Tablas();
		}
	}
	public class SentenciaInsert:SentenciaSelect{
		Tabla TablaBase;
		enum ConValuesOSelect {ConValues, ConSelect};
		ConValuesOSelect conQue;
		public SentenciaInsert(Tabla TablaBase){
			this.TablaBase=TablaBase;
		}
		public SentenciaInsert Select(params Campable[] CamposOTablas){
			conQue=ConValuesOSelect.ConSelect;
			foreach(Campable cc in CamposOTablas){
				foreach(Campo c in cc.Campos()){
					if(TablaBase.TieneElCampo(c) && Campos.FindIndex(delegate(Campo campo){return campo.NombreCampo==c.NombreCampo;})<0){
						Campos.Add(c);
					}
				}
			}
			return this;
		}
		public Sentencia Valores(params Campable[] CamposConValores){
			conQue=ConValuesOSelect.ConValues;
			foreach(Campable cc in CamposConValores){
				foreach(Campo c in cc.Campos()){
					if(TablaBase.TieneElCampo(c) && Campos.FindIndex(delegate(Campo campo){return campo.NombreCampo==c.NombreCampo;})<0){
						Campos.Add(c);
					}
				}
			}
			return this;
		}
		public override ListaSqlizable<Sqlizable> Partes(){
			ListaSqlizable<Sqlizable> todas=new ListaSqlizable<Sqlizable>();
			ListaSqlizable<Sqlizable> valores=new ListaSqlizable<Sqlizable>();
			todas.Add(new LiteralSql("INSERT INTO "));
			todas.Add(TablaBase);
			TablaBase.Alias=null;
			ParteSeparadora coma=new ParteSeparadora(" (",", ");
			ParteSeparadora vcoma=new ParteSeparadora("VALUES (",", ");
			foreach(Campo c in Campos){
				coma.AgregarEn(todas,new CampoReceptorInsert(c));
				if(conQue==ConValuesOSelect.ConValues){
					if(c is CampoAlias){
						vcoma.AgregarEn(valores,(c as CampoAlias).ExpresionBase);
					}else{
						vcoma.AgregarEn(valores,new ValorSql<object>(c.ValorSinTipo));
					}
				}
			}
			todas.Add(new LiteralSql(") "));
			if(conQue==ConValuesOSelect.ConSelect){
				todas.AddRange(base.Partes());
			}else{
				todas.AddRange(valores);
				todas.Add(new LiteralSql(")"));
			}
			return todas;
		}
	}
	public class Ejecutador:BasesDatos.EjecutadorSql{
		ListaSqlizable<Campo> CamposContexto=new ListaSqlizable<Campo>();
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
			Sentencia s=laSentencia.Clonate();
			StringBuilder rta=new StringBuilder();
			s.AsignarAlias();
			if(s is SentenciaUpdate){
				rta.Append("UPDATE ");
				SentenciaUpdate su=s as SentenciaUpdate;
				ConjuntoTablas suTablas=su.Tablas();
				if(suTablas.Count<=1 || !db.UpdateConJoin){
					su.TablaBase.Alias=null;
				}
				rta.Append(su.TablaBase.ToSql(db));
				string prefijoSet="";
				string sufijoSet="";
				if(db.UpdateConJoin){
					foreach(Tabla t in su.Tablas().Keys){
						if(t!=su.TablaBase){
							if(t.TablaRelacionada!=null && su.Tablas().Contains(t.TablaRelacionada)){
								rta.Append(" INNER JOIN "+t.ToSql(db)+" ON ");
								Separador and=new Separador(" AND ");
								foreach(Campo c in t.CamposPk()){
									rta.Append(and+t.CamposRelacionadosFk[c].UnicoCampo().Igual(c).ToSql(db));
								}
								rta.Append("\n");
							}
						}
					}
					foreach(Tabla t in s.Tablas().Keys){
						if(!t.LiberadaDelContextoDelEjecutador){
							foreach(Campo c in CamposContexto){
								if(t.TieneElCampo(c)){
									s.Where(t.CampoIndirecto(c).Igual(c.ValorSinTipo));
								}
							}
						}
					}
				}else{
					StringBuilder parteFrom=new StringBuilder();
					StringBuilder parteWhere=new StringBuilder();
					Separador and=new Separador(" WHERE "," AND ");
					Separador coma=new Separador(" FROM ",", ");
					foreach(Tabla t in su.Tablas().Keys){
						if(t!=su.TablaBase){
							if(t.TablaRelacionada!=null && su.Tablas().Contains(t.TablaRelacionada)){
								parteFrom.Append(coma+t.ToSql(db));
								foreach(Campo c in t.CamposPk()){
									parteWhere.Append(and+c.Igual(t.CamposRelacionadosFk[c]).ToSql(db));
								}
							}
						}
					}
					if(parteFrom.Length>0){
						prefijoSet="(SELECT ";
						sufijoSet=parteFrom.ToString()+parteWhere.ToString()+")";
					}
					if(!su.TablaBase.LiberadaDelContextoDelEjecutador){
						foreach(Campo c in CamposContexto){
							if(su.TablaBase.TieneElCampo(c)){
								s.Where(su.TablaBase.CampoIndirecto(c).Igual(c.ValorSinTipo));
							}
						}
					}
				}
				Separador setComa=new Separador(" SET ",",\n ");
				foreach(SentenciaUpdate.Sets p in su.ParteSet){
					rta.Append(setComa+p.CampoAsignado.ToSql(db)+"="+prefijoSet+p.ValorAsignar.ToSql(db)+sufijoSet);
				}
				foreach(Sqlizable p in su.PartesWhere()){
					rta.Append(p.ToSql(db));
				}
			}else{
				foreach(Tabla t in s.Tablas().Keys){
					System.Console.WriteLine("reviso "+t.NombreTabla);
					if(t.TablaRelacionada!=null && s.Tablas().Contains(t.TablaRelacionada)){
						System.Console.WriteLine("inducir relaci�n "+t.TablaRelacionada);
					// if(t.TablaRelacionada!=null){
						foreach(Campo c in t.CamposPk()){
							s.Where(c.Igual(t.CamposRelacionadosFk[c]));
						}
					}
				}
				foreach(Tabla t in s.Tablas().Keys){
					if(!t.LiberadaDelContextoDelEjecutador){
						foreach(Campo c in CamposContexto){
							if(t.TieneElCampo(c)){
								s.Where(t.CampoIndirecto(c).Igual(c.ValorSinTipo));
							}
						}
					}
				}
				foreach(Sqlizable p in s.Partes()){
					rta.Append(p.ToSql(db));
				}
			}
			rta.Append(";\n");
			return rta.ToString();
		}
	}
	public class ExpresionSql:Sqlizable{
		public bool TipoAgrupada=false;
		bool candidatoAGroupBy=false;
		public override bool CandidatoAGroupBy{ get{ return candidatoAGroupBy; } }
		public ListaSqlizable<Sqlizable> Partes=new ListaSqlizable<Sqlizable>();
		void CalcularTipo(){
			foreach(Sqlizable p in Partes){
				if(p is ExpresionSql){
					TipoAgrupada=TipoAgrupada || (p as ExpresionSql).TipoAgrupada;
				}
				candidatoAGroupBy=candidatoAGroupBy || p.CandidatoAGroupBy;
			}
		}
		public ExpresionSql(params Sqlizable[] Partes){
			this.Partes.AddRange(Partes);
			CalcularTipo();
		}
		public ExpresionSql(ListaSqlizable<Sqlizable> Partes){
			this.Partes=Partes;
			CalcularTipo();
		}
		public virtual ExpresionSql And(ExpresionSql otra){
			ListaSqlizable<Sqlizable> nueva=new ListaSqlizable<Sqlizable>();
			nueva.AddRange(Partes);
			nueva.Add(new LiteralSql("\n AND "));
			nueva.AddRange(otra.Partes);
			return new ExpresionSql(nueva);
		}
		public virtual ExpresionSql Or(ExpresionSql otra){
			ListaSqlizable<Sqlizable> nueva=new ListaSqlizable<Sqlizable>();
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
		public override ConjuntoTablas Tablas()
		{
			ConjuntoTablas rta=new ConjuntoTablas();
			foreach(Sqlizable s in Partes){
				rta.AddRange(s.Tablas());
			}
			return rta;
		}
		public Campo UnicoCampo(){
			Assert.AreEqual(1,Partes.Count);
			return (Campo)Partes[0];
		}
		public ExpresionSql Operado<T>(string OperadorTextual,T expresion){
			return new ExpresionSql(this,new LiteralSql(OperadorTextual),new ValorSql<T>(expresion));
		}
		public ExpresionSql Dividido<T2>(T2 Valor){
			return Operado<T2>("/",Valor);
		}
		public static ExpresionSql Agrupada<T>(string Operador, T expresion){
			ExpresionSql nueva=new ExpresionSql(new LiteralSql(Operador+"("),new ValorSql<T>(expresion),new LiteralSql(")"));
			nueva.TipoAgrupada=true;
			return nueva;
		}
		public static ExpresionSql Sum<T>(T expresion){
			return Agrupada("SUM",expresion);
		}
		public class SelectSuma:Sqlizable{
			Tabla TablaBase;
			public Campo CampoSumar;
			// ExpresionSql ExpresionWhere;
			/*
			public SelectSuma(Tabla TablaBase,Campo CampoSumar,ExpresionSql ExpresionWhere){
				this.TablaBase=TablaBase;
				this.CampoSumar=CampoSumar;
			}
			*/
			public SelectSuma(Tabla TablaBase,Campo CampoSumar)
			{
				this.TablaBase=TablaBase;
				this.CampoSumar=CampoSumar;
			}
			public override string ToSql(BaseDatos db)
			{
				// return "";
				StringBuilder rta=new StringBuilder();
				if(db.UpdateSelectSumViaDSum){
					Tabla TablaSumandis=CampoSumar.TablaContenedora;
					rta.Append("DSum('"+db.StuffCampo(CampoSumar.NombreCampo)+"','"
					           +db.StuffTabla(TablaSumandis.NombreTabla));
					if(TablaSumandis==TablaBase.TablaRelacionada){
						rta.Append("','");
						Separador and=new Separador(" AND ");
						foreach(Campo c in TablaBase.CamposPk()){
							rta.Append(and+db.StuffCampo(TablaBase.CamposRelacionadosFk[c].UnicoCampo().NombreCampo)+"=''' & "+c.ToSql(db)+" & '''");
						}
					}
					rta.Append("')");
					return rta.ToString();
				}else{
					Tabla TablaSumandis=CampoSumar.TablaContenedora;
					if(TablaSumandis.Alias==null){
						TablaSumandis.Alias="zz";
					}
					rta.Append("(SELECT SUM("+CampoSumar.ToSql(db)+") FROM "
					           +TablaSumandis.ToSql(db));
					if(TablaSumandis==TablaBase.TablaRelacionada){
						Separador and=new Separador(" WHERE "," AND ");
						foreach(Campo c in TablaBase.CamposPk()){
							rta.Append(and+TablaBase.CamposRelacionadosFk[c].ToSql(db)+"="+c.ToSql(db));
						}
					}
					rta.Append(")");
					return rta.ToString();
				}
			}
			public static implicit operator ExpresionSql(SelectSuma ss){
				return new ExpresionSql(ss);
			}
			public override bool CandidatoAGroupBy{ 
				get{
					return false;
				} 
			}
			public override ConjuntoTablas Tablas()
			{
				ConjuntoTablas rta=new ConjuntoTablas();
				// rta.Add(CampoSumar.TablaContenedora);
				return rta;
			}
		}
	}
}