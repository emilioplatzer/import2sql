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
	public class Sentencia:IElemento{
		public ListaElementos<ElementoTipado<bool>> ClausulaWhere=new ListaElementos<ElementoTipado<bool>>();
		public virtual string ToSql(BaseDatos db){
			var TablasIncluidas=Tablas(QueTablas.AlFrom);
			var rta=new StringBuilder();
			var whereComa=new Separador("\n WHERE ","\n AND ");
			foreach(var e in ClausulaWhere){
				whereComa.AgregarEn(rta,e.ToSql(db).Replace(" AND ","\n AND "));
			}
			foreach(Tabla t in TablasIncluidas.Keys){
				if(t.CamposContexto!=null 
				   && (t.TablaRelacionada==null
				       || !TablasIncluidas.Contiene(t.TablaRelacionada)))
				{
					foreach(Campo c in t.CamposContexto){
						if(t.ContieneMismoNombre(c)){
							whereComa.AgregarEn(rta,t.CampoIndirecto(c).ToSql(db)+"="+db.StuffValor(c.ValorSinTipo));
						}
					}
				}
			}
			return rta.ToString();
		}
		public virtual ConjuntoTablas Tablas(QueTablas queTablas){
			return new ConjuntoTablas();
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
		public ConjuntoTablas TablasQueEstanMasArriba;
		public SentenciaSelect(){
			ClausulaSelect=new ElementosClausulaSelect();
			ClausulaHaving=new ElementosClausulaHaving();
			TablasQueEstanMasArriba=new ConjuntoTablas();
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
			Separador whereAnd=new Separador("\n WHERE ","\n AND ");
			foreach(Tabla t in TablasIncluidas.Keys){
				if(t.TablaRelacionada!=null){
					if(TablasIncluidas.Contiene(t.TablaRelacionada)){
						foreach(System.Collections.Generic.KeyValuePair<Campo, IExpresion> par in t.CamposRelacionFk){
							whereAnd.AgregarEn(rta,par.Key.ToSql(db)+"="+par.Value.ToSql(db));
						}
					}else if(!TablasQueEstanMasArriba.Contiene(t.TablaRelacionada)){
						Falla.Detener("Falta la tabla "+t.TablaRelacionada.NombreTabla+" relacionada a "+t.NombreTabla+" en:\n"+rta.ToString());
					}
				}
			}
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
			ConjuntoTablas rta=base.Tablas(queTablas);
			rta.AddRange(ClausulaSelect.Tablas(queTablas));
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
			return new ConjuntoTablas(TablaBase);
		}
		public override string ToSql(BaseDatos db){
			var rta=new StringBuilder();
			rta.Append("UPDATE "+TablaBase.ToSql(db));
			var setComa=new Separador(" SET ",", ").AnchoLimitadoConIdentacion();
			foreach(var a in Asignaciones){
				setComa.AgregarEn(rta,a.CampoReceptor.ToSql(db)+"="+a.ExpresionBase.ToSql(db));
			}
			rta.Append(base.ToSql(db));
			return rta.ToString();
		}
	}
}
