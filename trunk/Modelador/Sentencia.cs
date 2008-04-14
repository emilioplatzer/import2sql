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
using PartesSql = System.Collections.Generic.List<Modelador.Sqlizable>;
using TablasSql = System.Collections.Generic.List<Modelador.Tabla>;
using CamposSql = System.Collections.Generic.List<Modelador.Campo>;

namespace Modelador
{
	public abstract class Sentencia{
		PartesSql ParteWhere=new PartesSql();
		public abstract PartesSql Partes();
		public abstract TablasSql Tablas();
		public Sentencia Where(ExpresionSql expresion){
			if(ParteWhere.Count>0){
				ParteWhere.Add(new LiteralSql("\n AND "));
			}
			ParteWhere.Add(expresion);
			return this;
		}
		public PartesSql PartesWhere(){
			PartesSql rta=new PartesSql();
			if(ParteWhere.Count>0){
				rta.Add(new LiteralSql("\n WHERE "));
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
		TablasSql TablasUsadas;
		System.Collections.Generic.Dictionary<string, string> AliasTablas=new System.Collections.Generic.Dictionary<string, string>();
		protected CamposSql Campos=new CamposSql();
		public SentenciaSelect(params Campo[] Campos){
			ParteSeparadora coma=new ParteSeparadora(", ");
			this.Campos.AddRange(Campos);
		}
		public override TablasSql Tablas(){
			if(TablasUsadas==null){
				TablasUsadas=new TablasSql();
				RegistrarTablas(Campos);
				RegistrarTablas(PartesWhere());
			}
			return TablasUsadas;
		}
		void RegistrarTablas(Campo c){
			ExpresionSql expresion=c.ExpresionBase;
			if(expresion!=null){
				RegistrarTablas(expresion);
			}
			RegistrarTabla(c.TablaContenedora);
		}
		void RegistrarTablas(CamposSql Campos){
			foreach(Campo c in Campos){
				RegistrarTablas(c);
			}
		}
		void RegistrarTablas(ExpresionSql expresion){
			RegistrarTablas(expresion.Partes);
		}
		void RegistrarTablas(PartesSql Partes){
			foreach(Sqlizable s in Partes){
				if(s is Campo){
					RegistrarTablas(s as Campo);
				}else if(s is ExpresionSql){
					RegistrarTablas(s as ExpresionSql);
				}
			}
		}
		void RegistrarTabla(Tabla t){
			if(t!=null){
				if(TablasUsadas.IndexOf(t)<0){
					TablasUsadas.Add(t);
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
			}
		}
		public override PartesSql Partes(){
			PartesSql todas=new PartesSql();
			todas.Add(new LiteralSql("SELECT "));
			{
				ParteSeparadora coma=new ParteSeparadora(", ");
				foreach(Campo c in Campos){
					ExpresionSql expresion=c.ExpresionBase;
					if(expresion!=null){
						coma.AgregarEn(todas,expresion,new LiteralSql(" AS "),new CampoReceptorInsert(c));
						foreach(Sqlizable s in c.ExpresionBase.Partes){
							if(s is Campo){
								Campo campo=s as Campo;
							}
						}
					}else{
						coma.AgregarEn(todas,c);
					}
				}
			}
			{
				ParteSeparadora coma=new ParteSeparadora(", ");
				todas.Add(new LiteralSql("\n FROM "));
				foreach(Tabla t in TablasUsadas){
					coma.AgregarEn(todas,t);
				}
			}
			todas.AddRange(PartesWhere());
			return todas;
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
				coma.AgregarEn(todas,new CampoReceptorInsert(c));
			}
			todas.Add(new LiteralSql(") "));
			todas.AddRange(base.Partes());
			return todas;
		}
	}
	public class Ejecutador:BasesDatos.EjecutadorSql{
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
		public ExpresionSql(PartesSql Partes){
			this.Partes=Partes;
		}
		public virtual ExpresionSql And(ExpresionSql otra){
			PartesSql nueva=new PartesSql();
			nueva.AddRange(Partes);
			nueva.Add(new LiteralSql("\n AND "));
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
}
