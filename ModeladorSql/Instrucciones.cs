/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 03/05/2008
 * Time: 01:20 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Text;

using Comunes;
using BasesDatos;

namespace ModeladorSql
{
	public delegate void ProcesamientoTabla(Tabla tabla);
	public delegate void ProcesamientoPar(System.Collections.Generic.KeyValuePair<Campo, IExpresion> par);
	public class Sentencia:IElemento{
		public ConjuntoTablas TablasQueEstanMasArriba;
		public ListaElementos<ElementoTipado<bool>> ClausulaWhere=new ListaElementos<ElementoTipado<bool>>();
		protected bool IncluirJoinEnWhere=true;
		public void ParaCadaJunta(ConjuntoTablas tablas,Tabla TablaBase,ProcesamientoTabla procesarTabla,ProcesamientoPar procesarPar){
			var TablasVistas=new ConjuntoTablas();
			var TablasARevisar=new ConjuntoTablas();
			TablasARevisar.AddRange(tablas);
			if(TablaBase!=null){
				TablasVistas.Add(TablaBase);
				TablasARevisar.Remove(TablaBase);
			}
			var TablasNoIncluidas=new ConjuntoTablas();
			int CantidadARevisar=TablasARevisar.Count;
			while(true){
				foreach(Tabla t in TablasARevisar.Keys){
					if(t.TablaRelacionada!=null && tablas.Contiene(t.TablaRelacionada)){
						if(TablasVistas.Contiene(t.TablaRelacionada)){
							procesarTabla(t);
							foreach(System.Collections.Generic.KeyValuePair<Campo, IExpresion> par in t.CamposRelacionFk){
								procesarPar(par);
							}
						}else{
							TablasNoIncluidas.Add(t);
							if((TablasQueEstanMasArriba==null || !TablasQueEstanMasArriba.Contiene(t.TablaRelacionada)) && !tablas.Contiene(t.TablaRelacionada)){
								Falla.Detener("Falta la tabla "+t.TablaRelacionada.NombreTabla+" relacionada a "+t.NombreTabla);
							}
						}
					}else{
						TablasVistas.Add(t);
					}
				}
				if(TablasNoIncluidas.Count==0){
			break;
				}else if(TablasNoIncluidas.Count==CantidadARevisar){
					Falla.Detener("FALLA AL ORDENAR EL JOIN "+TablasNoIncluidas.Count+"="+CantidadARevisar+": "+TablasNoIncluidas.ToString());
				}else{
					TablasARevisar=TablasNoIncluidas;
					TablasNoIncluidas=new ConjuntoTablas();
				}
			}
		}
		public virtual string ToSql(BaseDatos db){
			var rta=new StringBuilder();
			var whereAnd=new Separador("\n WHERE ","\n AND ");
			foreach(var e in ClausulaWhere){
				whereAnd.AgregarEn(rta,e.ToSql(db).Replace(" AND ","\n AND "));
			}
			var TablasIncluidas=Tablas(QueTablas.AlFrom);
			if(IncluirJoinEnWhere){
				ParaCadaJunta(TablasIncluidas, null, tabla => {},
					par => whereAnd.AgregarEn(rta,par.Key.ToSql(db)+"="+par.Value.ToSql(db))
				);
			}
			foreach(Tabla t in TablasIncluidas.Keys){
				if(t.CamposContexto!=null 
				   /*&& (t.TablaRelacionada==null
				       || !TablasIncluidas.Contiene(t.TablaRelacionada))*/)
				{
					foreach(Campo c in t.CamposContexto){
						if(t.ContieneMismoNombre(c)){
							whereAnd.AgregarEn(rta,t.CampoIndirecto(c).ToSql(db)+"="+db.StuffValor(c.ValorSinTipo));
						}
					}
				}
			}
			return rta.ToString();
		}
		public virtual ConjuntoTablas Tablas(QueTablas queTablas){
			return ClausulaWhere.Tablas(queTablas);
		}
		/*
		public void AsignarAlias(){
			ConjuntoTablas TablasAlias=Tablas(QueTablas.Aliasables);
			foreach(TablasAlias
		}
		*/
	}
	public static class ParaSentencias{
		public static TSentencia Where<TSentencia> (this TSentencia s, params ElementoTipado<bool>[] ExpresionesWhere) where TSentencia:Sentencia {
			s.ClausulaWhere.AddRange(ExpresionesWhere);
			return s;
		}
	}
	public class ElementosClausulaSelect:ListaElementos<IConCampos>{}
	public class ElementosClausula:ListaElementos<IElementoTipado<bool>>,IElemento{
		public string ToSql(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			Separador and=new Separador("\n AND ");
			foreach(IElementoTipado<bool> e in this){
				rta.Append(and+e.ToSql(db));
			}
			return rta.ToString();
		}
		/*
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			ConjuntoTablas rta=new ConjuntoTablas();
			foreach(IElementoTipado<bool> e in this){
				rta.AddRange(e.Tablas(queTablas));
			}
			return rta;
		}
		*/
	}
	public class ElementosClausulaWhere:ElementosClausula{}
	public class ElementosClausulaHaving:ElementosClausula{}
	public class SentenciaSelect:Sentencia{
		public ElementosClausulaSelect ClausulaSelect;
		public ElementosClausulaHaving ClausulaHaving;
		public SentenciaSelect(){
			ClausulaSelect=new ElementosClausulaSelect();
			ClausulaHaving=new ElementosClausulaHaving();
			TablasQueEstanMasArriba=new ConjuntoTablas();
		}
		public SentenciaSelect(params IConCampos[] campos)
			:this()
		{
			ClausulaSelect.AddRange(campos);
		}
		public SentenciaSelect Select(params IConCampos[] campos){
			ClausulaSelect.AddRange(campos);
			return this;
		}
		public SentenciaSelect Having(params IElementoTipado<bool>[] campos){
			ClausulaHaving.AddRange(campos);
			return this;
		}
		public override string ToSql(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			Separador selectComa=new Separador("SELECT ",", ").AnchoLimitadoConIdentacion();
			StringBuilder groupby=new StringBuilder();
			Separador groupbyComa=new Separador("\n GROUP BY ",", ").AnchoLimitadoConIdentacion();
			bool TieneAgrupadas=false;
			foreach(IConCampos campos in ClausulaSelect){
				foreach(Campo c in campos.Campos()){
					selectComa.AgregarEn(rta,c.ToSql(db));
					// IExpresion e=(c is ICampoAlias)?(c as ICampoAlias).ExpresionBase:c;
					IExpresion e=c.Expresion;
					TieneAgrupadas=TieneAgrupadas || e.EsAgrupada;
					if(e.CandidatoAGroupBy){
						groupbyComa.AgregarEn(groupby,e.ToSql(db));
					}
				}
			}
			Separador fromComa=new Separador("\n FROM ",", ").AnchoLimitadoConIdentacion();
			ConjuntoTablas TablasIncluidas=Tablas(QueTablas.AlFrom);
			foreach(Tabla t in TablasIncluidas.Keys){
				fromComa.AgregarEn(rta,t.ToSql(db));
			}
			rta.Append(base.ToSql(db));
			if(TieneAgrupadas){
				rta.Append(groupby);
			}
			Separador havingComa=new Separador("\n HAVING ","\n AND ").AnchoLimitadoConIdentacion();
			foreach(IExpresion e in ClausulaHaving){
				havingComa.AgregarEn(rta,e.ToSql(db));
			}
			return rta.ToString();
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas)
		{
			ConjuntoTablas rta=new ConjuntoTablas();
			rta.AddRange(ClausulaSelect.Tablas(queTablas));
			rta.AddRange(base.Tablas(queTablas));
			rta.AddRange(ClausulaHaving.Tablas(queTablas));
			return rta;
		}
	}
	public class SentenciaInsert:Sentencia{
		Tabla TablaBase;
		SentenciaSelect SentenciaSelectBase;
		ElementosClausulaSelect ValoresDirectos;
		public SentenciaInsert(Tabla TablaBase){
			this.TablaBase=TablaBase;
		}
		public SentenciaInsert Select(params IConCampos[] campos){
			Falla.SiNoEsNulo(ValoresDirectos,"En una sentencia insert no se puede poner un Select despues de Valores");
			SentenciaSelectBase=new SentenciaSelect();
			SentenciaSelectBase.Select(campos);
			return this;
		}
		public SentenciaInsert Having(params IElementoTipado<bool>[] campos){
			Falla.SiEsNulo(SentenciaSelectBase);
			SentenciaSelectBase.Having(campos);
			return this;
		}
		public SentenciaInsert Valores(params IConCampos[] campos){
			Falla.SiNoEsNulo(SentenciaSelectBase,"En una sentencia insert no se puede poner Valores despues de un Select");
			ValoresDirectos=new ElementosClausulaSelect();
			ValoresDirectos.AddRange(campos);
			return this;
		}
		public override string ToSql(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			rta.Append("INSERT INTO ");
			rta.Append(db.StuffTabla(TablaBase.NombreTabla));
			Separador coma=new Separador(" (",", ").AnchoLimitadoConIdentacion();
			ElementosClausulaSelect nuevaLista=new ElementosClausulaSelect();
			foreach(IConCampos e in ValoresDirectos??SentenciaSelectBase.ClausulaSelect){
				foreach(Campo c in e.Campos()){
					if(TablaBase.ContieneMismoNombre(c)){
						if(!nuevaLista.Exists(delegate(IConCampos campo){ return c.Nombre==campo.Campos()[0].Nombre; })){
							nuevaLista.Add(c);
							coma.AgregarEn(rta,db.StuffCampo(c.NombreCampo));
						}
					}
				}
			}
			if(SentenciaSelectBase==null){
				Separador valuesComa=new Separador(")\n VALUES (",", ").AnchoLimitadoConIdentacion();
				foreach(IExpresion e in nuevaLista){
					valuesComa.AgregarEn(rta,e.Expresion.ToSql(db));
				}
				rta.Append(")");
			}else{
				SentenciaSelectBase.ClausulaSelect=nuevaLista;
				rta.Append(")\n ");
				SentenciaSelectBase.TablasQueEstanMasArriba=new ConjuntoTablas(TablaBase);
				rta.Append(SentenciaSelectBase.ToSql(db));
			}
			return rta.ToString();
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			if(SentenciaSelectBase!=null){
				return SentenciaSelectBase.Tablas(queTablas);
			}else{
				return new ConjuntoTablas();
			}
		}
	}
	public class SentenciaUpdate:Sentencia{
		Tabla TablaBase;
		ListaElementos<ICampoAlias> Asignaciones=new ListaElementos<ICampoAlias>();
		public SentenciaUpdate(Tabla TablaBase,params ICampoAlias[] Asignaciones){
			this.TablaBase=TablaBase;
			this.Asignaciones.AddRange(Asignaciones);
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			var rta=new ConjuntoTablas();
			rta.Add(TablaBase);
			rta.AddRange(Asignaciones.Tablas(queTablas));
			rta.AddRange(base.Tablas(queTablas));
			return rta;
		}
		public override string ToSql(BaseDatos db){
			var rta=new StringBuilder();
			rta.Append("UPDATE "+TablaBase.ToSql(db));
			if(db.UpdateConJoin){
				IncluirJoinEnWhere=false;
				var tablas=Tablas(QueTablas.AlFrom);
				TablasQueEstanMasArriba=new ConjuntoTablas(TablaBase);
				var onAnd=new Separador(" ON "," AND ");
				ParaCadaJunta(tablas,TablaBase
				    , tabla => {rta.Append(" INNER JOIN "+tabla.ToSql(db)); onAnd.Reiniciar(); }
					, par => onAnd.AgregarEn(rta,par.Value.ToSql(db)+"="+par.Key.ToSql(db))
				);
			}
			var setComa=new Separador("\n SET ",", ").AnchoLimitadoConIdentacion();
			foreach(var a in Asignaciones){
				setComa.AgregarEn(rta,
					(db.UpdateConJoin?a.CampoReceptor.ToSql(db):db.StuffCampo(a.CampoReceptor.NombreCampo))
		            +"="+a.ExpresionBase.ToSql(db)
				);
			}
			if(!db.UpdateConJoin){
				IncluirJoinEnWhere=true;
				Separador fromComa=new Separador("\n FROM ",", ").AnchoLimitadoConIdentacion();
				ConjuntoTablas TablasIncluidas=Tablas(QueTablas.AlFrom);
				TablasIncluidas.Remove(TablaBase);
				foreach(Tabla t in TablasIncluidas.Keys){
					fromComa.AgregarEn(rta,t.ToSql(db));
				}
			}
			rta.Append(base.ToSql(db));
			return rta.ToString();
		}
	}
}
